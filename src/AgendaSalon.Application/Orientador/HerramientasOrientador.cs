using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using AgendaSalon.Application.Catalogo;
using AgendaSalon.Application.Comun;
using AgendaSalon.Application.Disponibilidad;
using AgendaSalon.Application.Reservas;
using AgendaSalon.Domain.Comun;

namespace AgendaSalon.Application.Orientador;

/// <summary>Definición de una herramienta independiente del proveedor de IA: el adaptador la traduce al SDK.</summary>
public sealed record DefinicionHerramienta(
    string Nombre,
    string Descripcion,
    IReadOnlyDictionary<string, JsonElement> Propiedades,
    IReadOnlyList<string> Requeridas);

/// <param name="Contenido">JSON que se devuelve al modelo.</param>
/// <param name="EsError">El modelo recibe el error para explicárselo al cliente o corregir la llamada.</param>
public sealed record ResultadoHerramienta(string Contenido, bool EsError, CitaDto? CitaCreada = null);

/// <summary>
/// Las acciones que el asistente puede hacer. Cada una llama a los mismos casos de uso que la web,
/// así que valen las mismas reglas: el asistente no puede saltarse ninguna validación.
/// </summary>
public sealed class HerramientasOrientador(
    ICatalogoService catalogo,
    IDisponibilidadService disponibilidad,
    IReservaService reservas)
{
    public const string VerServicios = "ver_servicios";
    public const string VerEstilistas = "ver_estilistas";
    public const string VerHorasLibres = "ver_horas_libres";
    public const string ReservarCita = "reservar_cita";
    public const string ConsultarCita = "consultar_cita";

    /// <summary>Límite de horas que se devuelven al modelo, para no gastar contexto con listas enormes.</summary>
    private const int MaximoHoras = 24;

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        // Tildes y ñ sin escapar: el JSON es para el modelo, no para una página HTML, y así usa menos tokens.
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Converters = { new JsonStringEnumConverter() },
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static IReadOnlyList<DefinicionHerramienta> Definiciones { get; } =
    [
        new(VerServicios,
            "Lista los servicios del salón con su id, categoría, descripción, duración en minutos y precio en pesos dominicanos (RD$). " +
            "Úsala antes de recomendar un servicio: no inventes servicios ni precios.",
            new Dictionary<string, JsonElement>(),
            []),
        new(VerEstilistas,
            "Lista los estilistas con su id y especialidad. Con servicio_id, solo los que realizan ese servicio.",
            new Dictionary<string, JsonElement>
            {
                ["servicio_id"] = Esquema(new { type = "integer", description = "Id del servicio (opcional)." })
            },
            []),
        new(VerHorasLibres,
            "Busca las horas libres de un servicio en una fecha. Devuelve la hora de inicio, la de fin y el estilista que atendería. " +
            "Sin estilista_id busca entre todos los que realizan el servicio.",
            new Dictionary<string, JsonElement>
            {
                ["servicio_id"] = Esquema(new { type = "integer", description = "Id del servicio." }),
                ["fecha"] = Esquema(new { type = "string", description = "Fecha en formato AAAA-MM-DD." }),
                ["estilista_id"] = Esquema(new { type = "integer", description = "Id del estilista preferido (opcional)." })
            },
            ["servicio_id", "fecha"]),
        new(ReservarCita,
            "Reserva una cita. Antes de llamarla, muestra al cliente un resumen (servicio, día, hora, estilista, precio, nombre y teléfono) " +
            "y espera a que diga claramente que sí. Pon cliente_confirmo en true solo si el cliente ya lo confirmó en este chat.",
            new Dictionary<string, JsonElement>
            {
                ["servicio_id"] = Esquema(new { type = "integer", description = "Id del servicio." }),
                ["inicio"] = Esquema(new { type = "string", description = "Inicio exacto tal como lo dio ver_horas_libres, formato AAAA-MM-DDTHH:mm." }),
                ["estilista_id"] = Esquema(new { type = "integer", description = "Id del estilista. Omítelo si al cliente le da igual." }),
                ["nombre"] = Esquema(new { type = "string", description = "Nombre del cliente." }),
                ["telefono"] = Esquema(new { type = "string", description = "Teléfono del cliente, 10 dígitos dominicanos." }),
                ["notas"] = Esquema(new { type = "string", description = "Notas para el estilista (opcional)." }),
                ["cliente_confirmo"] = Esquema(new { type = "boolean", description = "true solo si el cliente confirmó el resumen." })
            },
            ["servicio_id", "inicio", "nombre", "telefono", "cliente_confirmo"]),
        new(ConsultarCita,
            "Consulta una cita existente por su código de 6 caracteres. Para cancelar, el cliente debe usar la página Mi cita.",
            new Dictionary<string, JsonElement>
            {
                ["codigo"] = Esquema(new { type = "string", description = "Código de la cita." })
            },
            ["codigo"])
    ];

    /// <summary>Ejecuta una herramienta. Nunca lanza por errores de negocio o de entrada: los devuelve como resultado de error.</summary>
    public async Task<ResultadoHerramienta> EjecutarAsync(
        string nombre, IReadOnlyDictionary<string, JsonElement> entrada, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(entrada);
        try
        {
            return nombre switch
            {
                VerServicios => Exito(await catalogo.ListarServiciosAsync(ct)),
                VerEstilistas => Exito(await catalogo.ListarEstilistasAsync(EnteroOpcional(entrada, "servicio_id"), ct)),
                VerHorasLibres => await HorasLibresAsync(entrada, ct),
                ReservarCita => await ReservarAsync(entrada, ct),
                ConsultarCita => Exito(await reservas.ObtenerAsync(Texto(entrada, "codigo"), ct)),
                _ => Error($"La herramienta '{nombre}' no existe.")
            };
        }
        catch (Exception ex) when (ex is ReglaNegocioException or NoEncontradoException or ConflictoException or EntradaInvalidaException)
        {
            return Error(ex.Message);
        }
    }

    private async Task<ResultadoHerramienta> HorasLibresAsync(IReadOnlyDictionary<string, JsonElement> entrada, CancellationToken ct)
    {
        var huecos = await disponibilidad.ConsultarAsync(
            Entero(entrada, "servicio_id"), Fecha(entrada, "fecha"), EnteroOpcional(entrada, "estilista_id"), ct);

        var horas = huecos
            .Select(h => new { inicio = h.Inicio.ToString("yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture), fin = h.Fin.ToString("HH:mm", CultureInfo.InvariantCulture), estilista_id = h.EstilistaId, estilista = h.EstilistaNombre })
            .ToList();
        return Exito(new
        {
            total_horas = horas.Count,
            horas = horas.Take(MaximoHoras),
            nota = horas.Count > MaximoHoras ? $"Se muestran las primeras {MaximoHoras}. Pregunta al cliente qué parte del día prefiere." : null
        });
    }

    private async Task<ResultadoHerramienta> ReservarAsync(IReadOnlyDictionary<string, JsonElement> entrada, CancellationToken ct)
    {
        // Salvaguarda además de las instrucciones: sin confirmación explícita no se reserva.
        if (!entrada.TryGetValue("cliente_confirmo", out var confirmo) || confirmo.ValueKind != JsonValueKind.True)
            return Error("Falta la confirmación del cliente. Muéstrale el resumen y pregúntale si quiere reservar.");

        var cita = await reservas.CrearAsync(new CrearCitaRequest(
            Entero(entrada, "servicio_id"),
            EnteroOpcional(entrada, "estilista_id"),
            FechaHora(entrada, "inicio"),
            Texto(entrada, "nombre"),
            Texto(entrada, "telefono"),
            TextoOpcional(entrada, "notas")), ct);

        return new ResultadoHerramienta(Serializar(new { reservada = true, cita }), EsError: false, cita);
    }

    // ---- Lectura tolerante de la entrada del modelo ----

    private static int Entero(IReadOnlyDictionary<string, JsonElement> entrada, string campo) =>
        EnteroOpcional(entrada, campo) ?? throw new EntradaInvalidaException($"Falta '{campo}'.");

    private static int? EnteroOpcional(IReadOnlyDictionary<string, JsonElement> entrada, string campo)
    {
        if (!entrada.TryGetValue(campo, out var valor) || valor.ValueKind == JsonValueKind.Null)
            return null;
        if (valor.ValueKind == JsonValueKind.Number && valor.TryGetInt32(out var numero))
            return numero;
        if (valor.ValueKind == JsonValueKind.String && int.TryParse(valor.GetString(), NumberStyles.None, CultureInfo.InvariantCulture, out numero))
            return numero;
        throw new EntradaInvalidaException($"'{campo}' debe ser un número entero.");
    }

    private static string Texto(IReadOnlyDictionary<string, JsonElement> entrada, string campo) =>
        TextoOpcional(entrada, campo) ?? throw new EntradaInvalidaException($"Falta '{campo}'.");

    private static string? TextoOpcional(IReadOnlyDictionary<string, JsonElement> entrada, string campo) =>
        entrada.TryGetValue(campo, out var valor) && valor.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(valor.GetString())
            ? valor.GetString()
            : null;

    private static DateOnly Fecha(IReadOnlyDictionary<string, JsonElement> entrada, string campo) =>
        DateOnly.TryParseExact(Texto(entrada, campo), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var fecha)
            ? fecha
            : throw new EntradaInvalidaException($"'{campo}' debe tener el formato AAAA-MM-DD.");

    private static DateTime FechaHora(IReadOnlyDictionary<string, JsonElement> entrada, string campo) =>
        DateTime.TryParseExact(Texto(entrada, campo), ["yyyy-MM-ddTHH:mm", "yyyy-MM-ddTHH:mm:ss"], CultureInfo.InvariantCulture, DateTimeStyles.None, out var fecha)
            ? fecha
            : throw new EntradaInvalidaException($"'{campo}' debe tener el formato AAAA-MM-DDTHH:mm.");

    private static JsonElement Esquema(object esquema) => JsonSerializer.SerializeToElement(esquema);

    private static ResultadoHerramienta Exito(object datos) => new(Serializar(datos), EsError: false);

    private static ResultadoHerramienta Error(string mensaje) => new(Serializar(new { error = mensaje }), EsError: true);

    private static string Serializar(object datos) => JsonSerializer.Serialize(datos, Json);

    private sealed class EntradaInvalidaException(string mensaje) : Exception(mensaje);
}

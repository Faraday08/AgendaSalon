using System.Text.Json;
using System.Text.Json.Nodes;
using Anthropic;
using AgendaSalon.Application.Comun;
using AgendaSalon.Application.Orientador;
using AgendaSalon.Domain.Citas;
using AgendaSalon.Domain.Comun;
using AgendaSalon.Infrastructure;
using AgendaSalon.Infrastructure.Orientador;
using AgendaSalon.IntegrationTests.Soporte;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using static AgendaSalon.IntegrationTests.Soporte.EntornoAgenda;

namespace AgendaSalon.IntegrationTests;

/// <summary>Herramientas del asistente contra la base de datos real (SQLite en memoria).</summary>
public sealed class HerramientasOrientadorTests : IAsyncLifetime
{
    private EntornoAgenda _entorno = null!;
    private HerramientasOrientador _herramientas = null!;

    public async Task InitializeAsync()
    {
        _entorno = await CrearAsync();
        _herramientas = _entorno.Obtener<HerramientasOrientador>();
    }

    public async Task DisposeAsync() => await _entorno.DisposeAsync();

    private static Dictionary<string, JsonElement> Entrada(object valores) =>
        JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(JsonSerializer.Serialize(valores))!;

    private Dictionary<string, JsonElement> ReservaDeAna(bool confirmo) => Entrada(new
    {
        servicio_id = _entorno.CorteId,
        estilista_id = _entorno.AnaId,
        inicio = "2026-11-03T10:00",
        nombre = "María Rodríguez",
        telefono = "809-555-1234",
        cliente_confirmo = confirmo
    });

    [Fact]
    public async Task VerServicios_DevuelveElMenuConPrecios()
    {
        var resultado = await _herramientas.EjecutarAsync(HerramientasOrientador.VerServicios, Entrada(new { }));

        Assert.False(resultado.EsError);
        Assert.Contains("\"nombre\":\"Corte\"", resultado.Contenido, StringComparison.Ordinal);
        Assert.Contains("\"duracion_minutos\":60", resultado.Contenido, StringComparison.Ordinal);
    }

    [Fact]
    public async Task VerHorasLibres_AceptaElIdComoTextoYDevuelveLasHoras()
    {
        var resultado = await _herramientas.EjecutarAsync(HerramientasOrientador.VerHorasLibres,
            Entrada(new { servicio_id = _entorno.CorteId.ToString(System.Globalization.CultureInfo.InvariantCulture), fecha = "2026-11-03" }));

        Assert.False(resultado.EsError);
        Assert.Contains("2026-11-03T09:00", resultado.Contenido, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("03/11/2026")]
    [InlineData("mañana")]
    public async Task VerHorasLibres_ConFechaMalEscrita_DevuelveErrorSinLanzar(string fecha)
    {
        var resultado = await _herramientas.EjecutarAsync(HerramientasOrientador.VerHorasLibres,
            Entrada(new { servicio_id = _entorno.CorteId, fecha }));

        Assert.True(resultado.EsError);
        Assert.Contains("AAAA-MM-DD", resultado.Contenido, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ReservarCita_SinConfirmacionDelCliente_NoReserva()
    {
        var resultado = await _herramientas.EjecutarAsync(HerramientasOrientador.ReservarCita, ReservaDeAna(confirmo: false));

        Assert.True(resultado.EsError);
        Assert.Equal(0, await _entorno.Db.Citas.CountAsync());
    }

    [Fact]
    public async Task ReservarCita_ConfirmadaPorElCliente_CreaLaCitaYAvisaAlPanel()
    {
        var resultado = await _herramientas.EjecutarAsync(HerramientasOrientador.ReservarCita, ReservaDeAna(confirmo: true));

        Assert.False(resultado.EsError);
        Assert.NotNull(resultado.CitaCreada);
        Assert.Contains("\"estado\":\"Confirmada\"", resultado.Contenido, StringComparison.Ordinal);
        Assert.Equal(1, await _entorno.Db.Citas.CountAsync());
        Assert.Single(_entorno.Notificador.Enviadas);
    }

    [Fact]
    public async Task ReservarCita_EnHoraOcupada_DevuelveElMensajeDeNegocio()
    {
        await _herramientas.EjecutarAsync(HerramientasOrientador.ReservarCita, ReservaDeAna(confirmo: true));

        var resultado = await _herramientas.EjecutarAsync(HerramientasOrientador.ReservarCita, ReservaDeAna(confirmo: true));

        Assert.True(resultado.EsError);
        Assert.Contains("no está disponible", resultado.Contenido, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HerramientaDesconocida_DevuelveError()
    {
        var resultado = await _herramientas.EjecutarAsync("borrar_todo", Entrada(new { }));

        Assert.True(resultado.EsError);
    }
}

/// <summary>El ciclo completo del asistente con el SDK real de Anthropic hablando con una API simulada.</summary>
public sealed class OrientadorTests : IAsyncLifetime
{
    private const string ClaveDePrueba = "clave-de-prueba";

    private readonly ApiClaudeSimulada _claude = new();
    private EntornoAgenda _entorno = null!;
    private IOrientador _orientador = null!;

    public async Task InitializeAsync()
    {
        _entorno = await CrearAsync(services =>
        {
            services.AddOrientador();
            services.Configure<OpcionesOrientador>(_ => { });
            services.AddSingleton(new ClienteClaude(new AnthropicClient
            {
                ApiKey = ClaveDePrueba,
                BaseUrl = "https://api.anthropic.com",
                MaxRetries = 0,
                HttpClient = new HttpClient(_claude)
            }));
        });
        _orientador = _entorno.Obtener<IOrientador>();
    }

    public async Task DisposeAsync()
    {
        await _entorno.DisposeAsync();
        _claude.Dispose();
    }

    private static JsonArray Mensajes(JsonNode peticion) => peticion["messages"]!.AsArray();

    [Fact]
    public async Task Turno_EjecutaLaHerramientaYDevuelveElResultadoAClaude()
    {
        _claude
            .PideHerramienta("toolu_1", HerramientasOrientador.VerHorasLibres, new { servicio_id = _entorno.CorteId, fecha = "2026-11-03" })
            .Responde("El martes tengo libre a las 9:00 a. m. y a las 9:30 a. m.");

        var respuesta = await _orientador.ConversarAsync(new MensajeOrientadorRequest(null, "¿Tienen hora para un corte el martes?"));

        Assert.Equal("El martes tengo libre a las 9:00 a. m. y a las 9:30 a. m.", respuesta.Respuesta);
        Assert.Null(respuesta.CitaCreada);
        Assert.Equal(2, _claude.Peticiones.Count);

        // Segunda petición: mensaje del cliente, respuesta de Claude intacta (con su firma) y el resultado de la herramienta.
        var mensajes = Mensajes(_claude.Peticiones[1]);
        Assert.Equal(3, mensajes.Count);
        var asistente = mensajes[1]!["content"]!.AsArray();
        Assert.Equal("firma-de-prueba", asistente[0]!["signature"]!.GetValue<string>());
        var resultado = mensajes[2]!["content"]!.AsArray()[0]!;
        Assert.Equal("tool_result", resultado["type"]!.GetValue<string>());
        Assert.Equal("toolu_1", resultado["tool_use_id"]!.GetValue<string>());
        Assert.Contains("2026-11-03T09:00", resultado.ToJsonString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Peticion_UsaElModeloLasHerramientasYElRespaldoRecomendado()
    {
        _claude.Responde("¡Hola! ¿Qué servicio te interesa?");

        await _orientador.ConversarAsync(new MensajeOrientadorRequest(null, "Hola"));

        var peticion = _claude.Peticiones[0];
        Assert.Equal("claude-opus-5-5", peticion["model"]!.GetValue<string>());
        Assert.Equal("default", peticion["fallbacks"]!.GetValue<string>());
        Assert.Equal("low", peticion["output_config"]!["effort"]!.GetValue<string>());
        Assert.Equal(
            HerramientasOrientador.Definiciones.Select(d => d.Nombre),
            peticion["tools"]!.AsArray().Select(t => t!["name"]!.GetValue<string>()));
        Assert.Equal("ephemeral", peticion["system"]![0]!["cache_control"]!["type"]!.GetValue<string>());
        Assert.Contains("2026-11-02", peticion["system"]![1]!["text"]!.GetValue<string>(), StringComparison.Ordinal);

        var encabezados = _claude.Encabezados[0];
        Assert.Contains("server-side-fallback-2026-07-01", encabezados.Headers.GetValues("anthropic-beta"));
        Assert.Equal("api.anthropic.com", encabezados.RequestUri!.Host);
        // Desde la versión 13 del SDK (API 2026-10-21) la clave viaja como token Bearer.
        Assert.Equal("Bearer", encabezados.Headers.Authorization!.Scheme);
        Assert.Equal(ClaveDePrueba, encabezados.Headers.Authorization.Parameter);
    }

    [Fact]
    public async Task Turno_QueReserva_DevuelveLaCitaCreada()
    {
        _claude
            .PideHerramienta("toolu_1", HerramientasOrientador.ReservarCita, new
            {
                servicio_id = _entorno.CorteId,
                inicio = "2026-11-03T10:00",
                nombre = "María Rodríguez",
                telefono = "809-555-1234",
                cliente_confirmo = true
            })
            .Responde("¡Listo! Tu cita quedó reservada.");

        var respuesta = await _orientador.ConversarAsync(new MensajeOrientadorRequest(null, "Sí, confirmo"));

        Assert.NotNull(respuesta.CitaCreada);
        Assert.Equal(EstadoCita.Confirmada, respuesta.CitaCreada.Estado);
        Assert.Equal(1, await _entorno.Db.Citas.CountAsync());
    }

    [Fact]
    public async Task SegundoMensaje_ContinuaLaMismaConversacion()
    {
        _claude.Responde("¿Para qué día?").Responde("Perfecto.");

        var primera = await _orientador.ConversarAsync(new MensajeOrientadorRequest(null, "Quiero un corte"));
        var segunda = await _orientador.ConversarAsync(new MensajeOrientadorRequest(primera.ConversacionId, "El martes"));

        Assert.Equal(primera.ConversacionId, segunda.ConversacionId);
        Assert.Equal(3, Mensajes(_claude.Peticiones[1]).Count);
    }

    [Fact]
    public async Task Rechazo_DaUnaRespuestaAmableYNoGuardaElMensaje()
    {
        _claude.Rechaza().Responde("Claro, ¿qué servicio buscas?");

        var rechazada = await _orientador.ConversarAsync(new MensajeOrientadorRequest(null, "mensaje rechazado"));
        await _orientador.ConversarAsync(new MensajeOrientadorRequest(rechazada.ConversacionId, "Quiero un corte"));

        Assert.Contains("no te puedo ayudar", rechazada.Respuesta, StringComparison.Ordinal);
        var mensajes = Mensajes(_claude.Peticiones[1]);
        Assert.Single(mensajes);
        Assert.Equal("Quiero un corte", mensajes[0]!["content"]!.GetValue<string>());
    }

    [Fact]
    public async Task MensajeDemasiadoLargo_SeRechazaSinLlamarAClaude()
    {
        await Assert.ThrowsAsync<ReglaNegocioException>(() =>
            _orientador.ConversarAsync(new MensajeOrientadorRequest(null, new string('a', 501))));

        Assert.Empty(_claude.Peticiones);
    }

    [Fact]
    public async Task SinClaveDeApi_ElAsistenteNoEstaDisponible()
    {
        await using var sinClave = await CrearAsync(services =>
        {
            services.AddOrientador();
            services.AddSingleton(new ClienteClaude(null));
        });
        var orientador = sinClave.Obtener<IOrientador>();

        Assert.False(orientador.Disponible);
        await Assert.ThrowsAsync<ServicioNoDisponibleException>(() =>
            orientador.ConversarAsync(new MensajeOrientadorRequest(null, "Hola")));
    }
}

using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;
using Anthropic;
using Anthropic.Exceptions;
using Anthropic.Models.Beta;
using Anthropic.Models.Beta.Messages;
using AgendaSalon.Application.Abstracciones;
using AgendaSalon.Application.Comun;
using AgendaSalon.Application.Orientador;
using AgendaSalon.Application.Reservas;
using AgendaSalon.Domain.Comun;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AgendaSalon.Infrastructure.Orientador;

/// <summary>
/// Asistente orientador con Claude y uso de herramientas. Cada turno ejecuta el ciclo:
/// enviar la conversación → si Claude pide herramientas, ejecutarlas → devolver los resultados → repetir
/// hasta que responda con texto.
/// <para>
/// El historial se guarda en memoria del servidor y solo se le agregan mensajes al final, tal como llegan:
/// los bloques de razonamiento de Claude deben volver sin cambios en las peticiones siguientes.
/// En una instalación con varios servidores habría que moverlo a un almacenamiento compartido.
/// </para>
/// </summary>
internal sealed partial class OrientadorClaude(
    IOptions<OpcionesOrientador> opciones,
    ClienteClaude clienteClaude,
    IMemoryCache cache,
    IServiceScopeFactory alcances,
    IRelojSalon reloj,
    ILogger<OrientadorClaude> logger) : IOrientador, IDisposable
{
    private const string NoDisponible = "El asistente no está disponible en este momento. Puedes reservar directamente en la página.";
    private static readonly CultureInfo Espanol = CultureInfo.GetCultureInfo("es-DO");

    // fallbacks: "default" deja que Anthropic elija el modelo de respaldo según el motivo del rechazo.
    private static readonly BetaFallbacksParam FallbackRecomendado = new(JsonSerializer.SerializeToElement("default"));

    private readonly OpcionesOrientador _opciones = opciones.Value;
    private readonly ConcurrentDictionary<Guid, SemaphoreSlim> _candados = new();

    public bool Disponible => clienteClaude.Cliente is not null;

    public async Task<RespuestaOrientadorDto> ConversarAsync(MensajeOrientadorRequest solicitud, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(solicitud);
        var cliente = clienteClaude.Cliente ?? throw new ServicioNoDisponibleException(NoDisponible);

        var texto = solicitud.Mensaje?.Trim() ?? "";
        if (texto.Length == 0)
            throw new ReglaNegocioException("Escribe tu mensaje.");
        if (texto.Length > _opciones.LongitudMaximaMensaje)
            throw new ReglaNegocioException($"El mensaje no puede pasar de {_opciones.LongitudMaximaMensaje} caracteres.");

        var conversacion = ObtenerConversacion(solicitud.ConversacionId);
        var candado = _candados.GetOrAdd(conversacion.Id, _ => new SemaphoreSlim(1, 1));

        // Un mismo cliente no puede tener dos turnos a la vez: el historial debe quedar en orden.
        await candado.WaitAsync(ct);
        try
        {
            if (conversacion.MensajesDelCliente >= _opciones.MaximoMensajesPorConversacion)
                throw new ReglaNegocioException("Esta conversación llegó a su límite. Recarga la página para empezar otra.");

            conversacion.MensajesDelCliente++;
            conversacion.Historial.Add(new BetaUserMessageParam { Content = texto });
            return await ResolverTurnoAsync(cliente, conversacion, conversacion.Historial.Count - 1, ct);
        }
        finally
        {
            candado.Release();
        }
    }

    /// <param name="indiceMensajeCliente">Posición en el historial del mensaje que abrió este turno.</param>
    private async Task<RespuestaOrientadorDto> ResolverTurnoAsync(
        AnthropicClient cliente, Conversacion conversacion, int indiceMensajeCliente, CancellationToken ct)
    {
        await using var alcance = alcances.CreateAsyncScope();
        var herramientas = alcance.ServiceProvider.GetRequiredService<HerramientasOrientador>();
        CitaDto? citaCreada = null;

        for (var paso = 0; paso < _opciones.MaximoPasosPorTurno; paso++)
        {
            BetaMessage respuesta;
            try
            {
                respuesta = await cliente.Beta.Messages.Create(CrearParametros(conversacion), ct);
            }
            catch (AnthropicApiException ex)
            {
                ErrorDelModelo(logger, ex);
                DeshacerMensajeSinRespuesta(conversacion, indiceMensajeCliente);
                throw new ServicioNoDisponibleException(NoDisponible);
            }

            if (respuesta.StopReason == "refusal")
            {
                DeshacerMensajeSinRespuesta(conversacion, indiceMensajeCliente);
                return new RespuestaOrientadorDto(conversacion.Id,
                    "Con eso no te puedo ayudar. Si quieres, cuéntame qué servicio te interesa y te busco una hora.", citaCreada);
            }

            // La respuesta se agrega completa y sin cambios al historial.
            conversacion.Historial.Add(new BetaAssistantMessageParam { Content = AParametros(respuesta.Content) });

            var llamadas = respuesta.Content
                .Select(b => b.TryPickToolUse(out var uso) ? uso : null)
                .OfType<BetaToolUseBlock>()
                .ToList();

            if (respuesta.StopReason != "tool_use" || llamadas.Count == 0)
                return new RespuestaOrientadorDto(conversacion.Id, TextoDe(respuesta), citaCreada);

            // Todos los resultados van juntos en un solo mensaje, uno por cada llamada.
            List<BetaContentBlockParam> resultados = [];
            foreach (var llamada in llamadas)
            {
                var resultado = await herramientas.EjecutarAsync(llamada.Name, llamada.Input, ct);
                citaCreada ??= resultado.CitaCreada;
                resultados.Add(new BetaToolResultBlockParam
                {
                    ToolUseID = llamada.ID,
                    Content = resultado.Contenido,
                    IsError = resultado.EsError
                });
            }
            conversacion.Historial.Add(new BetaUserMessageParam { Content = resultados });
        }

        PasosAgotados(logger, conversacion.Id);
        return new RespuestaOrientadorDto(conversacion.Id,
            "Me está costando completar eso. ¿Me lo puedes decir de otra forma, o prefieres reservar directamente en la página?", citaCreada);
    }

    private MessageCreateParams CrearParametros(Conversacion conversacion) => new()
    {
        Model = _opciones.Modelo,
        MaxTokens = _opciones.MaximoTokensRespuesta,
        // Si los filtros de seguridad rechazan una petición, Anthropic la reintenta con el modelo de respaldo recomendado.
        Betas = [AnthropicBeta.ServerSideFallback2026_07_01],
        Fallbacks = FallbackRecomendado,
        OutputConfig = new BetaOutputConfig { Effort = _opciones.Esfuerzo },
        System = new List<BetaTextBlockParam>
        {
            new() { Text = InstruccionesOrientador.Sistema, CacheControl = new BetaCacheControlEphemeral() },
            new() { Text = ContextoDelMomento() }
        },
        Tools = HerramientasOrientador.Definiciones.Select(AHerramienta).ToList(),
        Messages = conversacion.Historial
    };

    private string ContextoDelMomento()
    {
        var ahora = reloj.Ahora;
        return $"Ahora en el salón es {ahora.ToString("dddd d 'de' MMMM 'de' yyyy, h:mm tt", Espanol)} " +
               $"(fecha {ahora:yyyy-MM-dd}). Usa esta fecha para entender \"hoy\", \"mañana\" o \"el sábado\".";
    }

    private static BetaToolUnion AHerramienta(DefinicionHerramienta definicion) => new BetaTool
    {
        Name = definicion.Nombre,
        Description = definicion.Descripcion,
        InputSchema = new()
        {
            Properties = definicion.Propiedades.ToDictionary(p => p.Key, p => p.Value),
            Required = [.. definicion.Requeridas]
        }
    };

    /// <summary>Convierte el contenido de la respuesta en parámetros para reenviarlo. El SDK no trae una conversión directa.</summary>
    private static List<BetaContentBlockParam> AParametros(IReadOnlyList<BetaContentBlock> bloques)
    {
        List<BetaContentBlockParam> parametros = [];
        foreach (var bloque in bloques)
        {
            if (bloque.TryPickText(out var texto))
                parametros.Add(new BetaTextBlockParam { Text = texto.Text });
            else if (bloque.TryPickThinking(out var razonamiento))
                parametros.Add(new BetaThinkingBlockParam { Thinking = razonamiento.Thinking, Signature = razonamiento.Signature });
            else if (bloque.TryPickRedactedThinking(out var oculto))
                parametros.Add(new BetaRedactedThinkingBlockParam { Data = oculto.Data });
            else if (bloque.TryPickToolUse(out var uso))
                parametros.Add(new BetaToolUseBlockParam { ID = uso.ID, Name = uso.Name, Input = uso.Input });
            else if (bloque.TryPickFallback(out var respaldo))
                parametros.Add(BetaFallbackBlockParam.FromRawUnchecked(respaldo.RawData));
        }
        return parametros;
    }

    private static string TextoDe(BetaMessage respuesta)
    {
        var texto = string.Join("\n\n", respuesta.Content
            .Select(b => b.TryPickText(out var t) ? t.Text.Trim() : null)
            .OfType<string>()
            .Where(t => t.Length > 0));

        return texto.Length > 0 ? texto : "¿En qué más te puedo ayudar?";
    }

    /// <summary>
    /// Si el mensaje del cliente quedó sin ninguna respuesta, se quita para que pueda reintentar.
    /// Si ya hubo llamadas a herramientas no se toca nada: el historial solo crece por el final y cada
    /// llamada debe conservar su resultado.
    /// </summary>
    private static void DeshacerMensajeSinRespuesta(Conversacion conversacion, int indiceMensajeCliente)
    {
        if (conversacion.Historial.Count - 1 != indiceMensajeCliente)
            return;

        conversacion.Historial.RemoveAt(indiceMensajeCliente);
        conversacion.MensajesDelCliente--;
    }

    private Conversacion ObtenerConversacion(Guid? id)
    {
        var expiracion = new MemoryCacheEntryOptions { SlidingExpiration = TimeSpan.FromMinutes(_opciones.MinutosDeInactividad) };
        expiracion.RegisterPostEvictionCallback((clave, _, _, _) =>
        {
            if (clave is Guid idExpirado && _candados.TryRemove(idExpirado, out var candado))
                candado.Dispose();
        });

        // Un id desconocido o vencido empieza una conversación nueva (con un id nuevo, para no reutilizar el del cliente).
        if (id is Guid existente && cache.TryGetValue(existente, out Conversacion? conversacion) && conversacion is not null)
            return conversacion;

        var nueva = new Conversacion(Guid.NewGuid());
        cache.Set(nueva.Id, nueva, expiracion);
        return nueva;
    }

    public void Dispose()
    {
        foreach (var candado in _candados.Values)
            candado.Dispose();
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "La API de Claude respondió con un error.")]
    private static partial void ErrorDelModelo(ILogger logger, Exception ex);

    [LoggerMessage(Level = LogLevel.Warning, Message = "La conversación {ConversacionId} agotó los pasos de herramientas permitidos en un turno.")]
    private static partial void PasosAgotados(ILogger logger, Guid conversacionId);

    private sealed class Conversacion(Guid id)
    {
        public Guid Id { get; } = id;
        public List<BetaMessageParam> Historial { get; } = [];
        public int MensajesDelCliente { get; set; }
    }
}

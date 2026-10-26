namespace AgendaSalon.Infrastructure.Orientador;

/// <summary>Sección "Orientador" de la configuración.</summary>
public sealed class OpcionesOrientador
{
    /// <summary>
    /// Clave de la API de Claude. Nunca va en appsettings.json: se configura con user-secrets
    /// (Orientador:ClaveApi) o con la variable de entorno ANTHROPIC_API_KEY. Sin clave, el asistente queda desactivado.
    /// </summary>
    public string? ClaveApi { get; set; }

    /// <summary>
    /// Dirección de la API. Se fija aquí para que una variable de entorno suelta (ANTHROPIC_BASE_URL)
    /// no desvíe las peticiones, y con ellas la clave, a otro servidor.
    /// </summary>
    public string UrlApi { get; set; } = "https://api.anthropic.com";

    public string Modelo { get; set; } = "claude-opus-5-5";

    /// <summary>Profundidad de razonamiento: low, medium, high, xhigh o max. Para un chat corto basta con low.</summary>
    public string Esfuerzo { get; set; } = "low";

    public int MaximoTokensRespuesta { get; set; } = 2048;

    /// <summary>Llamadas a herramientas encadenadas permitidas en un mismo turno, para cortar bucles.</summary>
    public int MaximoPasosPorTurno { get; set; } = 8;

    /// <summary>Mensajes del cliente por conversación. Limita el costo de una sola sesión.</summary>
    public int MaximoMensajesPorConversacion { get; set; } = 30;

    public int LongitudMaximaMensaje { get; set; } = 500;

    /// <summary>Una conversación sin actividad se descarta pasados estos minutos.</summary>
    public int MinutosDeInactividad { get; set; } = 30;
}

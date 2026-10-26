using AgendaSalon.Application.Reservas;

namespace AgendaSalon.Application.Orientador;

/// <summary>Mensaje del cliente al asistente. Sin <see cref="ConversacionId"/> empieza una conversación nueva.</summary>
public sealed record MensajeOrientadorRequest(Guid? ConversacionId, string Mensaje);

/// <param name="CitaCreada">La cita que el asistente reservó en este turno, si reservó alguna.</param>
public sealed record RespuestaOrientadorDto(Guid ConversacionId, string Respuesta, CitaDto? CitaCreada);

/// <summary>
/// Asistente que orienta al cliente: le ayuda a elegir el servicio, busca horas libres y reserva
/// cuando el cliente lo confirma. La implementación con el modelo de IA vive en Infrastructure.
/// </summary>
public interface IOrientador
{
    /// <summary>False si no hay credenciales del modelo configuradas.</summary>
    bool Disponible { get; }

    Task<RespuestaOrientadorDto> ConversarAsync(MensajeOrientadorRequest solicitud, CancellationToken ct = default);
}

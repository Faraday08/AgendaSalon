using AgendaSalon.Application.Panel;

namespace AgendaSalon.Application.Abstracciones;

/// <summary>
/// Avisa en tiempo real al panel del salón. Es de mejor esfuerzo: si falla, la operación ya está guardada
/// y la implementación solo registra el error.
/// </summary>
public interface INotificadorAgenda
{
    Task CitaCreadaAsync(CitaPanelDto cita, CancellationToken ct = default);

    Task CitaActualizadaAsync(CitaPanelDto cita, CancellationToken ct = default);
}

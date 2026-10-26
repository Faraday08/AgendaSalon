using AgendaSalon.Application.Abstracciones;
using AgendaSalon.Application.Panel;
using Microsoft.AspNetCore.SignalR;

namespace AgendaSalon.Api.TiempoReal;

/// <summary>Envía los cambios de la agenda a los administradores y al estilista de la cita.</summary>
internal sealed partial class NotificadorAgendaSignalR(
    IHubContext<AgendaHub, IClienteAgenda> hub,
    ILogger<NotificadorAgendaSignalR> logger) : INotificadorAgenda
{
    public Task CitaCreadaAsync(CitaPanelDto cita, CancellationToken ct = default) =>
        EnviarAsync(cita, nameof(IClienteAgenda.CitaCreada), clientes => clientes.CitaCreada(cita));

    public Task CitaActualizadaAsync(CitaPanelDto cita, CancellationToken ct = default) =>
        EnviarAsync(cita, nameof(IClienteAgenda.CitaActualizada), clientes => clientes.CitaActualizada(cita));

    private async Task EnviarAsync(CitaPanelDto cita, string evento, Func<IClienteAgenda, Task> enviar)
    {
        try
        {
            await enviar(hub.Clients.Groups(GruposAgenda.Administradores, GruposAgenda.DeEstilista(cita.EstilistaId)));
        }
#pragma warning disable CA1031 // La notificación es de mejor esfuerzo: la cita ya está guardada y no debe fallar la petición.
        catch (Exception ex)
#pragma warning restore CA1031
        {
            NoSePudoNotificar(logger, ex, evento, cita.Codigo);
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "No se pudo enviar {Evento} de la cita {Codigo} al panel.")]
    private static partial void NoSePudoNotificar(ILogger logger, Exception ex, string evento, string codigo);
}

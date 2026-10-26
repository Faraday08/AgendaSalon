using AgendaSalon.Application.Abstracciones;
using AgendaSalon.Application.Panel;
using AgendaSalon.Domain.Usuarios;

namespace AgendaSalon.IntegrationTests.Soporte;

internal sealed class RelojFijo(DateTime ahora) : IRelojSalon
{
    public DateTime Ahora => ahora;
}

/// <summary>Permite simular quién está usando el panel en cada prueba.</summary>
internal sealed class UsuarioActualFalso : IUsuarioActual
{
    public bool EstaAutenticado { get; private set; }
    public int Id { get; private set; }
    public RolUsuario Rol { get; private set; }
    public int? EstilistaId { get; private set; }

    public void ComoAdministrador() => (EstaAutenticado, Id, Rol, EstilistaId) = (true, 1, RolUsuario.Administrador, null);

    public void ComoEstilista(int estilistaId) => (EstaAutenticado, Id, Rol, EstilistaId) = (true, 2, RolUsuario.Estilista, estilistaId);
}

internal sealed record Notificacion(string Evento, CitaPanelDto Cita);

/// <summary>Guarda las notificaciones en lugar de enviarlas.</summary>
internal sealed class NotificadorFalso : INotificadorAgenda
{
    private readonly List<Notificacion> _enviadas = [];

    public IReadOnlyList<Notificacion> Enviadas => _enviadas;

    public Task CitaCreadaAsync(CitaPanelDto cita, CancellationToken ct = default)
    {
        _enviadas.Add(new Notificacion(nameof(CitaCreadaAsync), cita));
        return Task.CompletedTask;
    }

    public Task CitaActualizadaAsync(CitaPanelDto cita, CancellationToken ct = default)
    {
        _enviadas.Add(new Notificacion(nameof(CitaActualizadaAsync), cita));
        return Task.CompletedTask;
    }
}

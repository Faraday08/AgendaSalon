using System.Globalization;
using AgendaSalon.Api.Seguridad;
using AgendaSalon.Application.Panel;
using AgendaSalon.Domain.Usuarios;
using AgendaSalon.Infrastructure.Seguridad;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace AgendaSalon.Api.TiempoReal;

/// <summary>Eventos que el servidor envía al panel. El cliente se suscribe con estos mismos nombres.</summary>
public interface IClienteAgenda
{
    Task CitaCreada(CitaPanelDto cita);

    Task CitaActualizada(CitaPanelDto cita);
}

public static class GruposAgenda
{
    public const string Administradores = "administradores";

    public static string DeEstilista(int estilistaId) =>
        string.Create(CultureInfo.InvariantCulture, $"estilista-{estilistaId}");
}

/// <summary>
/// Canal en tiempo real del panel. Al conectarse, cada usuario entra al grupo que le corresponde:
/// los administradores reciben todo y cada estilista solo lo de su agenda.
/// </summary>
[Authorize(Policy = PoliticasAutorizacion.Personal)]
public sealed class AgendaHub : Hub<IClienteAgenda>
{
    public const string Ruta = "/hubs/agenda";

    public override async Task OnConnectedAsync()
    {
        var usuario = Context.User;
        if (usuario?.IsInRole(nameof(RolUsuario.Administrador)) == true)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, GruposAgenda.Administradores);
        }
        else if (int.TryParse(usuario?.FindFirst(ClaimsAgenda.EstilistaId)?.Value, NumberStyles.None, CultureInfo.InvariantCulture, out var estilistaId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, GruposAgenda.DeEstilista(estilistaId));
        }

        await base.OnConnectedAsync();
    }
}

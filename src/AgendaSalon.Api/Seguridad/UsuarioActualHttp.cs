using System.Globalization;
using System.Security.Claims;
using AgendaSalon.Application.Abstracciones;
using AgendaSalon.Domain.Usuarios;
using AgendaSalon.Infrastructure.Seguridad;

namespace AgendaSalon.Api.Seguridad;

/// <summary>Lee el usuario actual de los claims del token JWT de la petición.</summary>
internal sealed class UsuarioActualHttp(IHttpContextAccessor accesor) : IUsuarioActual
{
    private ClaimsPrincipal? Principal => accesor.HttpContext?.User;

    public bool EstaAutenticado => Principal?.Identity?.IsAuthenticated == true;

    public int Id => int.Parse(Leer(ClaimsAgenda.Id), NumberStyles.None, CultureInfo.InvariantCulture);

    public RolUsuario Rol => Enum.Parse<RolUsuario>(Leer(ClaimsAgenda.Rol));

    public int? EstilistaId =>
        int.TryParse(Principal?.FindFirst(ClaimsAgenda.EstilistaId)?.Value, NumberStyles.None, CultureInfo.InvariantCulture, out var id)
            ? id
            : null;

    private string Leer(string tipo) =>
        Principal?.FindFirst(tipo)?.Value ?? throw new InvalidOperationException($"El token no tiene el claim '{tipo}'.");
}

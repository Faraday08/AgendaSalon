using AgendaSalon.Domain.Usuarios;

namespace AgendaSalon.Api.Seguridad;

public static class PoliticasAutorizacion
{
    /// <summary>Cualquier miembro del personal con sesión iniciada.</summary>
    public const string Personal = nameof(Personal);

    /// <summary>Solo administradores.</summary>
    public const string Administrador = nameof(RolUsuario.Administrador);

    /// <summary>Límite de intentos de inicio de sesión por dirección IP.</summary>
    public const string LimiteLogin = "login";
}

using AgendaSalon.Application.Abstracciones;
using AgendaSalon.Application.Comun;
using AgendaSalon.Domain.Usuarios;

namespace AgendaSalon.Application.Autenticacion;

public sealed record IniciarSesionRequest(string Correo, string Contrasena);

public sealed record SesionDto(string Token, DateTime ExpiraEnUtc, string Nombre, RolUsuario Rol, int? EstilistaId);

public interface IAutenticacionService
{
    Task<SesionDto> IniciarSesionAsync(IniciarSesionRequest solicitud, CancellationToken ct = default);
}

internal sealed class AutenticacionService(
    IUsuarioRepositorio usuarios,
    IHasherContrasenas hasher,
    IGeneradorToken generadorToken) : IAutenticacionService
{
    // El mismo mensaje para cualquier fallo, para no revelar si el correo existe.
    private const string CredencialesInvalidas = "Correo o contraseña incorrectos.";

    public async Task<SesionDto> IniciarSesionAsync(IniciarSesionRequest solicitud, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(solicitud);

        var usuario = Correo.TryCrear(solicitud.Correo, out var correo)
            ? await usuarios.ObtenerPorCorreoAsync(correo, ct)
            : null;

        // Se verifica siempre, aunque el usuario no exista, para que todas las respuestas tarden lo mismo.
        var contrasenaCorrecta = hasher.Verificar(usuario?.HashContrasena, solicitud.Contrasena ?? "");
        if (usuario is null || !contrasenaCorrecta || !usuario.Activo)
            throw new NoAutorizadoException(CredencialesInvalidas);

        var token = generadorToken.Generar(usuario);
        return new SesionDto(token.Token, token.ExpiraEnUtc, usuario.Nombre, usuario.Rol, usuario.EstilistaId);
    }
}

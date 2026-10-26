using AgendaSalon.Application.Abstracciones;
using AgendaSalon.Application.Comun;
using AgendaSalon.Domain.Estilistas;
using AgendaSalon.Domain.Usuarios;

namespace AgendaSalon.Application.Usuarios;

public sealed record CrearCuentaEstilistaRequest(string Nombre, string Correo, string Contrasena, int EstilistaId);

public sealed record UsuarioDto(int Id, string Nombre, string Correo, RolUsuario Rol, int? EstilistaId);

public interface IUsuarioService
{
    /// <summary>Solo un administrador puede crear cuentas.</summary>
    Task<UsuarioDto> CrearCuentaEstilistaAsync(CrearCuentaEstilistaRequest solicitud, CancellationToken ct = default);
}

internal sealed class UsuarioService(
    IUsuarioRepositorio usuarios,
    IEstilistaRepositorio estilistas,
    IHasherContrasenas hasher,
    IUnidadDeTrabajo unidadDeTrabajo,
    IUsuarioActual usuarioActual,
    IRelojSalon reloj) : IUsuarioService
{
    public async Task<UsuarioDto> CrearCuentaEstilistaAsync(CrearCuentaEstilistaRequest solicitud, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(solicitud);
        if (!usuarioActual.EstaAutenticado || usuarioActual.Rol != RolUsuario.Administrador)
            throw new ProhibidoException("Solo un administrador puede crear cuentas.");

        var correo = Correo.Crear(solicitud.Correo);
        PoliticaContrasenas.Validar(solicitud.Contrasena);

        var estilista = await estilistas.ObtenerActivoAsync(solicitud.EstilistaId, ct)
            ?? throw new NoEncontradoException("El estilista no existe o está inactivo.");
        if (await usuarios.ExisteCorreoAsync(correo, ct))
            throw new ConflictoException("Ya existe una cuenta con ese correo.");
        if (await usuarios.ExisteCuentaParaEstilistaAsync(estilista.Id, ct))
            throw new ConflictoException("Ese estilista ya tiene una cuenta.");

        var usuario = Usuario.CrearParaEstilista(
            solicitud.Nombre, correo, hasher.Hashear(solicitud.Contrasena), estilista, reloj.Ahora);
        usuarios.Agregar(usuario);
        await unidadDeTrabajo.GuardarCambiosAsync(ct);

        return new UsuarioDto(usuario.Id, usuario.Nombre, usuario.Correo.Valor, usuario.Rol, usuario.EstilistaId);
    }
}

using AgendaSalon.Domain.Comun;
using AgendaSalon.Domain.Estilistas;

namespace AgendaSalon.Domain.Usuarios;

/// <summary>Cuenta del personal del salón para entrar al panel.</summary>
public sealed class Usuario : Entidad
{
    public const int LongitudMaximaNombre = 80;
    public const int LongitudMaximaHash = 256;

    private Usuario() { }

    public string Nombre { get; private set; } = null!;
    public Correo Correo { get; private set; } = null!;

    /// <summary>Hash de la contraseña. La contraseña en claro nunca llega al dominio.</summary>
    public string HashContrasena { get; private set; } = null!;

    public RolUsuario Rol { get; private set; }

    /// <summary>Solo para el rol Estilista: el estilista cuya agenda gestiona esta cuenta.</summary>
    public int? EstilistaId { get; private set; }

    public Estilista? Estilista { get; private set; }
    public bool Activo { get; private set; } = true;
    public DateTime CreadoEn { get; private set; }

    public static Usuario CrearAdministrador(string nombre, Correo correo, string hashContrasena, DateTime ahora) =>
        Crear(nombre, correo, hashContrasena, RolUsuario.Administrador, null, ahora);

    public static Usuario CrearParaEstilista(
        string nombre, Correo correo, string hashContrasena, Estilista estilista, DateTime ahora)
    {
        ArgumentNullException.ThrowIfNull(estilista);
        if (!estilista.Activo)
            throw new ReglaNegocioException("No se puede crear una cuenta para un estilista inactivo.");

        return Crear(nombre, correo, hashContrasena, RolUsuario.Estilista, estilista, ahora);
    }

    public void CambiarContrasena(string hashContrasena) => HashContrasena = ValidarHash(hashContrasena);

    public void Desactivar() => Activo = false;

    private static Usuario Crear(
        string nombre, Correo correo, string hashContrasena, RolUsuario rol, Estilista? estilista, DateTime ahora)
    {
        ArgumentNullException.ThrowIfNull(correo);
        return new Usuario
        {
            Nombre = Texto.Requerido(nombre, LongitudMaximaNombre, "el nombre"),
            Correo = correo,
            HashContrasena = ValidarHash(hashContrasena),
            Rol = rol,
            Estilista = estilista,
            EstilistaId = estilista?.Id is > 0 ? estilista.Id : null,
            CreadoEn = ahora
        };
    }

    private static string ValidarHash(string hash) =>
        !string.IsNullOrWhiteSpace(hash) && hash.Length <= LongitudMaximaHash
            ? hash
            : throw new ArgumentException("El hash de la contraseña no es válido.", nameof(hash));
}

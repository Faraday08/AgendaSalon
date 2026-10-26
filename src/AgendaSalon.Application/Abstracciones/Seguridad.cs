using AgendaSalon.Domain.Usuarios;

namespace AgendaSalon.Application.Abstracciones;

public interface IHasherContrasenas
{
    string Hashear(string contrasena);

    /// <summary>
    /// Con <paramref name="hash"/> null compara contra un hash ficticio y devuelve false, para que
    /// un correo inexistente tarde lo mismo que uno existente y no se pueda adivinar qué cuentas hay.
    /// </summary>
    bool Verificar(string? hash, string contrasena);
}

public sealed record TokenAcceso(string Token, DateTime ExpiraEnUtc);

public interface IGeneradorToken
{
    TokenAcceso Generar(Usuario usuario);
}

/// <summary>El usuario del panel que hace la petición actual.</summary>
public interface IUsuarioActual
{
    bool EstaAutenticado { get; }
    int Id { get; }
    RolUsuario Rol { get; }

    /// <summary>Solo para el rol Estilista.</summary>
    int? EstilistaId { get; }
}

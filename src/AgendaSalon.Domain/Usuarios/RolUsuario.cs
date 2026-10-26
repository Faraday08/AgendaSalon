namespace AgendaSalon.Domain.Usuarios;

/// <summary>Los valores coinciden con los Id de la tabla catálogo RolesUsuario.</summary>
public enum RolUsuario : byte
{
    /// <summary>Ve y gestiona la agenda de todo el salón y crea cuentas.</summary>
    Administrador = 1,

    /// <summary>Ve y gestiona solo sus propias citas.</summary>
    Estilista = 2
}

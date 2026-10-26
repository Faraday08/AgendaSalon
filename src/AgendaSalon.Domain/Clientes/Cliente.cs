using AgendaSalon.Domain.Comun;

namespace AgendaSalon.Domain.Clientes;

/// <summary>Persona que reserva. Se identifica por su teléfono, que es único.</summary>
public sealed class Cliente : Entidad
{
    public const int LongitudMaximaNombre = 80;

    private Cliente() { }

    public string Nombre { get; private set; } = null!;
    public Telefono Telefono { get; private set; } = null!;

    public static Cliente Registrar(string? nombre, Telefono telefono)
    {
        ArgumentNullException.ThrowIfNull(telefono);
        return new Cliente
        {
            Nombre = Texto.Requerido(nombre, LongitudMaximaNombre, "el nombre"),
            Telefono = telefono
        };
    }
}

using AgendaSalon.Domain.Comun;

namespace AgendaSalon.Domain.Servicios;

/// <summary>Agrupa los servicios del menú: corte, color, uñas...</summary>
public sealed class CategoriaServicio : Entidad
{
    public const int LongitudMaximaNombre = 40;

    private CategoriaServicio() { }

    public string Nombre { get; private set; } = null!;

    public static CategoriaServicio Crear(string nombre) =>
        new() { Nombre = Texto.Requerido(nombre, LongitudMaximaNombre, "el nombre de la categoría") };
}

using AgendaSalon.Domain.Comun;

namespace AgendaSalon.Domain.Servicios;

public sealed class Servicio : Entidad
{
    public const int LongitudMaximaNombre = 80;
    public const int LongitudMaximaDescripcion = 300;

    private Servicio() { }

    public string Nombre { get; private set; } = null!;
    public string Descripcion { get; private set; } = null!;
    public int CategoriaId { get; private set; }
    public CategoriaServicio Categoria { get; private set; } = null!;
    public int DuracionMinutos { get; private set; }
    public decimal Precio { get; private set; }
    public bool Activo { get; private set; } = true;

    public static Servicio Crear(
        string nombre, string descripcion, CategoriaServicio categoria, int duracionMinutos, decimal precio)
    {
        ArgumentNullException.ThrowIfNull(categoria);
        if (duracionMinutos <= 0)
            throw new ReglaNegocioException("La duración del servicio debe ser mayor que cero.");

        return new Servicio
        {
            Nombre = Texto.Requerido(nombre, LongitudMaximaNombre, "el nombre del servicio"),
            Descripcion = Texto.Requerido(descripcion, LongitudMaximaDescripcion, "la descripción"),
            Categoria = categoria,
            DuracionMinutos = duracionMinutos,
            Precio = ValidarPrecio(precio)
        };
    }

    /// <summary>Las citas ya reservadas conservan el precio que tenían al reservar.</summary>
    public void CambiarPrecio(decimal precio) => Precio = ValidarPrecio(precio);

    public void Desactivar() => Activo = false;

    private static decimal ValidarPrecio(decimal precio) =>
        precio >= 0 ? precio : throw new ReglaNegocioException("El precio no puede ser negativo.");
}

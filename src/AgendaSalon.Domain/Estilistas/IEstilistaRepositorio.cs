namespace AgendaSalon.Domain.Estilistas;

public interface IEstilistaRepositorio
{
    /// <summary>
    /// Estilistas activos con sus servicios y horarios, ordenados por nombre.
    /// Con <paramref name="servicioId"/>, solo los que realizan ese servicio.
    /// </summary>
    Task<IReadOnlyList<Estilista>> ListarActivosAsync(int? servicioId = null, CancellationToken ct = default);
}

namespace AgendaSalon.Domain.Servicios;

public interface IServicioRepositorio
{
    /// <summary>Servicios activos con su categoría, ordenados por categoría y nombre.</summary>
    Task<IReadOnlyList<Servicio>> ListarActivosAsync(CancellationToken ct = default);

    Task<Servicio?> ObtenerActivoAsync(int id, CancellationToken ct = default);
}

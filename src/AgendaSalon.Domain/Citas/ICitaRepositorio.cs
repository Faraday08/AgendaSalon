using AgendaSalon.Domain.Agenda;

namespace AgendaSalon.Domain.Citas;

public interface ICitaRepositorio
{
    /// <summary>Incluye cliente, servicio y estilista.</summary>
    Task<Cita?> ObtenerPorCodigoAsync(CodigoCita codigo, CancellationToken ct = default);

    Task<bool> ExisteCodigoAsync(CodigoCita codigo, CancellationToken ct = default);

    /// <summary>Horarios ocupados por citas no canceladas, agrupados por estilista.</summary>
    Task<ILookup<int, Intervalo>> ObtenerOcupacionAsync(
        IReadOnlyCollection<int> estilistaIds, DateOnly fecha, CancellationToken ct = default);

    /// <summary>
    /// Citas que empiezan entre <paramref name="desde"/> (incluido) y <paramref name="hasta"/> (excluido),
    /// con cliente, servicio y estilista, ordenadas por hora. Con <paramref name="estilistaId"/>, solo las de ese estilista.
    /// </summary>
    Task<IReadOnlyList<Cita>> ListarEntreAsync(DateTime desde, DateTime hasta, int? estilistaId, CancellationToken ct = default);

    void Agregar(Cita cita);
}

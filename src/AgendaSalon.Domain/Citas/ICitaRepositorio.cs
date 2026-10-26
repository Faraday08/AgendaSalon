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

    void Agregar(Cita cita);
}

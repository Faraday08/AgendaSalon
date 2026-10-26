using AgendaSalon.Domain.Agenda;
using AgendaSalon.Domain.Citas;
using AgendaSalon.Domain.Clientes;
using AgendaSalon.Domain.Estilistas;
using AgendaSalon.Domain.Servicios;
using Microsoft.EntityFrameworkCore;

namespace AgendaSalon.Infrastructure.Persistencia.Repositorios;

// Los repositorios devuelven entidades rastreadas: la aplicación las modifica o las asocia a citas nuevas
// dentro de la misma unidad de trabajo.

internal sealed class ClienteRepositorio(AgendaDbContext db) : IClienteRepositorio
{
    public Task<Cliente?> ObtenerPorTelefonoAsync(Telefono telefono, CancellationToken ct = default) =>
        db.Clientes.FirstOrDefaultAsync(c => c.Telefono == telefono, ct);

    public void Agregar(Cliente cliente) => db.Clientes.Add(cliente);
}

internal sealed class ServicioRepositorio(AgendaDbContext db) : IServicioRepositorio
{
    public async Task<IReadOnlyList<Servicio>> ListarActivosAsync(CancellationToken ct = default) =>
        await db.Servicios
            .Include(s => s.Categoria)
            .Where(s => s.Activo)
            .OrderBy(s => s.Categoria.Nombre).ThenBy(s => s.Nombre)
            .ToListAsync(ct);

    public Task<Servicio?> ObtenerActivoAsync(int id, CancellationToken ct = default) =>
        db.Servicios.FirstOrDefaultAsync(s => s.Id == id && s.Activo, ct);
}

internal sealed class EstilistaRepositorio(AgendaDbContext db) : IEstilistaRepositorio
{
    public async Task<IReadOnlyList<Estilista>> ListarActivosAsync(int? servicioId = null, CancellationToken ct = default)
    {
        var consulta = db.Estilistas
            .Include(e => e.Servicios)
            .Include(e => e.Horarios)
            .Where(e => e.Activo);

        if (servicioId is not null)
            consulta = consulta.Where(e => e.Servicios.Any(s => s.Id == servicioId));

        return await consulta
            .OrderBy(e => e.Nombre)
            .AsSplitQuery()
            .ToListAsync(ct);
    }
}

internal sealed class CitaRepositorio(AgendaDbContext db) : ICitaRepositorio
{
    public Task<Cita?> ObtenerPorCodigoAsync(CodigoCita codigo, CancellationToken ct = default) =>
        db.Citas
            .Include(c => c.Cliente)
            .Include(c => c.Servicio)
            .Include(c => c.Estilista)
            .FirstOrDefaultAsync(c => c.Codigo == codigo, ct);

    public Task<bool> ExisteCodigoAsync(CodigoCita codigo, CancellationToken ct = default) =>
        db.Citas.AnyAsync(c => c.Codigo == codigo, ct);

    public async Task<ILookup<int, Intervalo>> ObtenerOcupacionAsync(
        IReadOnlyCollection<int> estilistaIds, DateOnly fecha, CancellationToken ct = default)
    {
        var desde = fecha.ToDateTime(TimeOnly.MinValue);
        var hasta = desde.AddDays(1);

        var ocupadas = await db.Citas
            .AsNoTracking()
            .Where(c => estilistaIds.Contains(c.EstilistaId)
                && c.Estado != EstadoCita.Cancelada
                && c.Inicio < hasta && c.Fin > desde)
            .Select(c => new { c.EstilistaId, c.Inicio, c.Fin })
            .ToListAsync(ct);

        return ocupadas.ToLookup(c => c.EstilistaId, c => new Intervalo(c.Inicio, c.Fin));
    }

    public void Agregar(Cita cita) => db.Citas.Add(cita);
}

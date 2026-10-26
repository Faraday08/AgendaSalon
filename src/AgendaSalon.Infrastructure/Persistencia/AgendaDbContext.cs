using AgendaSalon.Domain.Citas;
using AgendaSalon.Domain.Clientes;
using AgendaSalon.Domain.Estilistas;
using AgendaSalon.Domain.Servicios;
using Microsoft.EntityFrameworkCore;

namespace AgendaSalon.Infrastructure.Persistencia;

public sealed class AgendaDbContext(DbContextOptions<AgendaDbContext> options) : DbContext(options)
{
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<CategoriaServicio> CategoriasServicio => Set<CategoriaServicio>();
    public DbSet<Servicio> Servicios => Set<Servicio>();
    public DbSet<Estilista> Estilistas => Set<Estilista>();
    public DbSet<Cita> Citas => Set<Cita>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AgendaDbContext).Assembly);
}

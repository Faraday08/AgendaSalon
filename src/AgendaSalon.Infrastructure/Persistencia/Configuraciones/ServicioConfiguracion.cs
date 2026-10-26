using AgendaSalon.Domain.Servicios;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgendaSalon.Infrastructure.Persistencia.Configuraciones;

internal sealed class CategoriaServicioConfiguracion : IEntityTypeConfiguration<CategoriaServicio>
{
    public void Configure(EntityTypeBuilder<CategoriaServicio> builder)
    {
        builder.ToTable("CategoriasServicio");

        builder.Property(c => c.Nombre).HasMaxLength(CategoriaServicio.LongitudMaximaNombre);
        builder.HasIndex(c => c.Nombre).IsUnique();
    }
}

internal sealed class ServicioConfiguracion : IEntityTypeConfiguration<Servicio>
{
    public void Configure(EntityTypeBuilder<Servicio> builder)
    {
        builder.ToTable("Servicios", t =>
        {
            t.HasCheckConstraint("CK_Servicios_DuracionMinutos", "[DuracionMinutos] > 0");
            t.HasCheckConstraint("CK_Servicios_Precio", "[Precio] >= 0");
        });

        builder.Property(s => s.Nombre).HasMaxLength(Servicio.LongitudMaximaNombre);
        builder.Property(s => s.Descripcion).HasMaxLength(Servicio.LongitudMaximaDescripcion);
        builder.Property(s => s.Precio).HasPrecision(10, 2);
        builder.HasIndex(s => s.Nombre).IsUnique();

        builder.HasOne(s => s.Categoria)
            .WithMany()
            .HasForeignKey(s => s.CategoriaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

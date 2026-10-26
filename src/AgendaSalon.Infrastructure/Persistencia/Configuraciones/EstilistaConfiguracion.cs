using AgendaSalon.Domain.Estilistas;
using AgendaSalon.Domain.Servicios;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgendaSalon.Infrastructure.Persistencia.Configuraciones;

internal sealed class EstilistaConfiguracion : IEntityTypeConfiguration<Estilista>
{
    public void Configure(EntityTypeBuilder<Estilista> builder)
    {
        builder.ToTable("Estilistas");

        builder.Property(e => e.Nombre).HasMaxLength(Estilista.LongitudMaximaNombre);
        builder.Property(e => e.Especialidad).HasMaxLength(Estilista.LongitudMaximaEspecialidad);

        builder.HasMany(e => e.Horarios)
            .WithOne()
            .HasForeignKey(h => h.EstilistaId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(e => e.Horarios).UsePropertyAccessMode(PropertyAccessMode.Field);

        // Tabla puente: qué servicios realiza cada estilista.
        builder.HasMany(e => e.Servicios)
            .WithMany()
            .UsingEntity<Dictionary<string, object>>(
                "EstilistaServicios",
                servicio => servicio.HasOne<Servicio>().WithMany().HasForeignKey("ServicioId").OnDelete(DeleteBehavior.Cascade),
                estilista => estilista.HasOne<Estilista>().WithMany().HasForeignKey("EstilistaId").OnDelete(DeleteBehavior.Cascade),
                tabla => tabla.HasKey("EstilistaId", "ServicioId"));
        builder.Navigation(e => e.Servicios).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class HorarioLaboralConfiguracion : IEntityTypeConfiguration<HorarioLaboral>
{
    public void Configure(EntityTypeBuilder<HorarioLaboral> builder)
    {
        builder.ToTable("HorariosLaborales", t =>
        {
            t.HasCheckConstraint("CK_HorariosLaborales_Dia", "[Dia] BETWEEN 0 AND 6");
            t.HasCheckConstraint("CK_HorariosLaborales_Rango", "[Fin] > [Inicio]");
        });

        builder.Property(h => h.Dia).HasConversion<byte>();
        builder.HasIndex(h => new { h.EstilistaId, h.Dia });
    }
}

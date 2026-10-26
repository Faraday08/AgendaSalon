using AgendaSalon.Domain.Citas;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgendaSalon.Infrastructure.Persistencia.Configuraciones;

internal sealed class CitaConfiguracion : IEntityTypeConfiguration<Cita>
{
    public void Configure(EntityTypeBuilder<Cita> builder)
    {
        builder.ToTable("Citas", t => t.HasCheckConstraint("CK_Citas_Horario", "[Fin] > [Inicio]"));

        builder.Property(c => c.Codigo)
            .HasConversion(c => c.Valor, valor => CodigoCita.Desde(valor))
            .HasMaxLength(CodigoCita.Longitud)
            .IsFixedLength()
            .IsUnicode(false);
        builder.HasIndex(c => c.Codigo).IsUnique();

        builder.Property(c => c.PrecioAcordado).HasPrecision(10, 2);
        builder.Property(c => c.Notas).HasMaxLength(Cita.LongitudMaximaNotas);
        builder.Ignore(c => c.Horario);

        builder.Property(c => c.Estado)
            .HasColumnName("EstadoId")
            .HasConversion<byte>();
        builder.HasOne<EstadoCitaRegistro>()
            .WithMany()
            .HasForeignKey(c => c.Estado)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.Cliente).WithMany().HasForeignKey(c => c.ClienteId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(c => c.Servicio).WithMany().HasForeignKey(c => c.ServicioId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(c => c.Estilista).WithMany().HasForeignKey(c => c.EstilistaId).OnDelete(DeleteBehavior.Restrict);

        // Búsqueda de choques de horario por estilista y día.
        builder.HasIndex(c => new { c.EstilistaId, c.Inicio });
    }
}

/// <summary>
/// Catálogo de estados de cita. El dominio trabaja con el enum <see cref="EstadoCita"/>;
/// esta tabla da integridad referencial y nombres legibles a quien consulte la base.
/// </summary>
internal sealed class EstadoCitaRegistro
{
    public EstadoCita Id { get; init; }
    public required string Nombre { get; init; }
}

internal sealed class EstadoCitaRegistroConfiguracion : IEntityTypeConfiguration<EstadoCitaRegistro>
{
    public void Configure(EntityTypeBuilder<EstadoCitaRegistro> builder)
    {
        builder.ToTable("EstadosCita");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasConversion<byte>().ValueGeneratedNever();
        builder.Property(e => e.Nombre).HasMaxLength(30);

        builder.HasData(
            new EstadoCitaRegistro { Id = EstadoCita.Confirmada, Nombre = "Confirmada" },
            new EstadoCitaRegistro { Id = EstadoCita.Cancelada, Nombre = "Cancelada" },
            new EstadoCitaRegistro { Id = EstadoCita.Completada, Nombre = "Completada" },
            new EstadoCitaRegistro { Id = EstadoCita.NoAsistio, Nombre = "No asistió" });
    }
}

using AgendaSalon.Domain.Usuarios;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgendaSalon.Infrastructure.Persistencia.Configuraciones;

internal sealed class UsuarioConfiguracion : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        // Un administrador no tiene estilista; una cuenta de estilista siempre lo tiene.
        builder.ToTable("Usuarios", t => t.HasCheckConstraint(
            "CK_Usuarios_RolEstilista",
            "([RolId] = 1 AND [EstilistaId] IS NULL) OR ([RolId] = 2 AND [EstilistaId] IS NOT NULL)"));

        builder.Property(u => u.Nombre).HasMaxLength(Usuario.LongitudMaximaNombre);
        builder.Property(u => u.Correo)
            .HasConversion(c => c.Valor, valor => Correo.Crear(valor))
            .HasMaxLength(Correo.LongitudMaxima)
            .IsUnicode(false);
        builder.HasIndex(u => u.Correo).IsUnique();
        builder.Property(u => u.HashContrasena).HasMaxLength(Usuario.LongitudMaximaHash).IsUnicode(false);

        builder.Property(u => u.Rol).HasColumnName("RolId").HasConversion<byte>();
        builder.HasOne<RolUsuarioRegistro>().WithMany().HasForeignKey(u => u.Rol).OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(u => u.Estilista).WithMany().HasForeignKey(u => u.EstilistaId).OnDelete(DeleteBehavior.Restrict);
        // Como máximo una cuenta por estilista.
        builder.HasIndex(u => u.EstilistaId).IsUnique().HasFilter("[EstilistaId] IS NOT NULL");
    }
}

/// <summary>Catálogo de roles, con la misma idea que <see cref="EstadoCitaRegistro"/>.</summary>
internal sealed class RolUsuarioRegistro
{
    public RolUsuario Id { get; init; }
    public required string Nombre { get; init; }
}

internal sealed class RolUsuarioRegistroConfiguracion : IEntityTypeConfiguration<RolUsuarioRegistro>
{
    public void Configure(EntityTypeBuilder<RolUsuarioRegistro> builder)
    {
        builder.ToTable("RolesUsuario");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasConversion<byte>().ValueGeneratedNever();
        builder.Property(r => r.Nombre).HasMaxLength(30);

        builder.HasData(
            new RolUsuarioRegistro { Id = RolUsuario.Administrador, Nombre = "Administrador" },
            new RolUsuarioRegistro { Id = RolUsuario.Estilista, Nombre = "Estilista" });
    }
}

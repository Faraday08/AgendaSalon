using AgendaSalon.Domain.Clientes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgendaSalon.Infrastructure.Persistencia.Configuraciones;

internal sealed class ClienteConfiguracion : IEntityTypeConfiguration<Cliente>
{
    public void Configure(EntityTypeBuilder<Cliente> builder)
    {
        builder.ToTable("Clientes");

        builder.Property(c => c.Nombre).HasMaxLength(Cliente.LongitudMaximaNombre);
        builder.Property(c => c.Telefono)
            .HasConversion(t => t.Valor, valor => Telefono.Crear(valor))
            .HasMaxLength(Telefono.Digitos)
            .IsFixedLength()
            .IsUnicode(false);

        builder.HasIndex(c => c.Telefono).IsUnique();
    }
}

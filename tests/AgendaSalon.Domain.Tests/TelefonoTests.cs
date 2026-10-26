using AgendaSalon.Domain.Clientes;
using AgendaSalon.Domain.Comun;

namespace AgendaSalon.Domain.Tests;

public class TelefonoTests
{
    [Theory]
    [InlineData("809-555-1234")]
    [InlineData("(809) 555 1234")]
    [InlineData("+1 809 555 1234")]
    [InlineData("8095551234")]
    public void FormatosValidos_SeNormalizanALosDiezDigitos(string texto)
    {
        var telefono = Telefono.Crear(texto);

        Assert.Equal("8095551234", telefono.Valor);
        Assert.Equal("809-555-1234", telefono.ToString());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("555-1234")]
    [InlineData("809-555-12345")]
    [InlineData("2 809 555 1234")]
    public void FormatosInvalidos_SeRechazan(string? texto)
    {
        Assert.Throws<ReglaNegocioException>(() => Telefono.Crear(texto));
    }

    [Fact]
    public void DosTelefonosConElMismoNumero_SonIguales()
    {
        Assert.Equal(Telefono.Crear("809-555-1234"), Telefono.Crear("+1 (809) 555 1234"));
    }
}

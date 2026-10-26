using AgendaSalon.Domain.Citas;
using AgendaSalon.Domain.Clientes;
using AgendaSalon.Domain.Comun;
using AgendaSalon.Domain.Estilistas;
using AgendaSalon.Domain.Servicios;
using static AgendaSalon.Domain.Tests.Datos;

namespace AgendaSalon.Domain.Tests;

public class EntidadesTests
{
    [Fact]
    public void Estilista_JornadaQueSeCruzaConOtra_SeRechaza()
    {
        var estilista = EstilistaDeMartes();

        Assert.Throws<ReglaNegocioException>(() =>
            estilista.AgregarJornada(DayOfWeek.Tuesday, new TimeOnly(11, 0), new TimeOnly(14, 0)));
    }

    [Fact]
    public void Estilista_JornadaQueTerminaAntesDeEmpezar_SeRechaza()
    {
        var estilista = Estilista.Contratar("Ana", "Cortes");

        Assert.Throws<ReglaNegocioException>(() =>
            estilista.AgregarJornada(DayOfWeek.Monday, new TimeOnly(12, 0), new TimeOnly(9, 0)));
    }

    [Fact]
    public void Estilista_AsignarElMismoServicioDosVeces_NoLoDuplica()
    {
        var corte = Corte();
        var estilista = EstilistaDeMartes(corte);

        estilista.AsignarServicio(corte);

        Assert.Single(estilista.Servicios);
    }

    [Theory]
    [InlineData(0, 500)]
    [InlineData(30, -1)]
    public void Servicio_DuracionOPrecioInvalidos_SeRechaza(int duracion, int precio)
    {
        Assert.Throws<ReglaNegocioException>(() =>
            Servicio.Crear("Corte", "Corte", CategoriaServicio.Crear("Corte"), duracion, precio));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Cliente_SinNombre_SeRechaza(string nombre)
    {
        Assert.Throws<ReglaNegocioException>(() => Cliente.Registrar(nombre, Telefono.Crear("809-555-1234")));
    }

    [Theory]
    [InlineData(" abc234 ", true)]
    [InlineData("ABC10O", false)]
    [InlineData("ABC23", false)]
    [InlineData(null, false)]
    public void CodigoCita_AceptaSoloElFormatoValido(string? texto, bool esValido)
    {
        Assert.Equal(esValido, CodigoCita.TryCrear(texto, out _));
    }
}

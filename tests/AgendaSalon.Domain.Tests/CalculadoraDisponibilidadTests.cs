using AgendaSalon.Domain.Agenda;
using AgendaSalon.Domain.Estilistas;
using static AgendaSalon.Domain.Tests.Datos;

namespace AgendaSalon.Domain.Tests;

public class CalculadoraDisponibilidadTests
{
    private static IReadOnlyList<DateTime> HorasLibres(
        int duracionMinutos, IEnumerable<Intervalo>? ocupados = null, DateTime? ahora = null, DateOnly? fecha = null) =>
        CalculadoraDisponibilidad.HorasLibres(
            fecha ?? Martes, duracionMinutos, EstilistaDeMartes().Horarios, ocupados ?? [], ahora ?? Ahora, Reglas);

    [Fact]
    public void SinCitas_OfreceTodasLasHorasQueCabenEnLaJornada()
    {
        Assert.Equal([ElMartesA(9), ElMartesA(9, 30), ElMartesA(10), ElMartesA(10, 30), ElMartesA(11)], HorasLibres(60));
    }

    [Fact]
    public void ExcluyeLasHorasQueChocanConUnaCita()
    {
        Intervalo[] ocupados = [new(ElMartesA(10), ElMartesA(10, 45))];

        // 9:00 terminaría a las 10:00, justo cuando empieza la cita: se permite.
        Assert.Equal([ElMartesA(9), ElMartesA(11)], HorasLibres(60, ocupados));
    }

    [Fact]
    public void ServicioMasLargoQueLaJornada_NoTieneHoras()
    {
        Assert.Empty(HorasLibres(240));
    }

    [Fact]
    public void DiaQueNoTrabaja_NoTieneHoras()
    {
        Assert.Empty(HorasLibres(30, fecha: Martes.AddDays(1)));
    }

    [Fact]
    public void RespetaLaAnticipacionMinima()
    {
        // A las 9:10 la primera hora permitida es 10:10, así que la primera opción es 10:30.
        Assert.Equal([ElMartesA(10, 30), ElMartesA(11)], HorasLibres(60, ahora: ElMartesA(9, 10)));
    }

    [Fact]
    public void FechasPasadasOFueraDelLimite_NoTienenHoras()
    {
        var martesAnterior = Martes.AddDays(-7);
        var martesMuyLejano = Martes.AddDays(7 * 6);

        Assert.Empty(HorasLibres(30, fecha: martesAnterior));
        Assert.Empty(HorasLibres(30, fecha: martesMuyLejano));
    }

    [Fact]
    public void JornadaPartida_OfreceHorasEnAmbosTramos()
    {
        var estilista = Estilista.Contratar("Ana", "Cortes");
        estilista.AgregarJornada(DayOfWeek.Tuesday, new TimeOnly(14, 0), new TimeOnly(15, 0));
        estilista.AgregarJornada(DayOfWeek.Tuesday, new TimeOnly(9, 0), new TimeOnly(10, 0));

        var libres = CalculadoraDisponibilidad.HorasLibres(Martes, 60, estilista.Horarios, [], Ahora, Reglas);

        Assert.Equal([ElMartesA(9), ElMartesA(14)], libres);
    }
}

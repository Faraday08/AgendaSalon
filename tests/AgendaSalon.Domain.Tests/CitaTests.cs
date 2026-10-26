using AgendaSalon.Domain.Citas;
using AgendaSalon.Domain.Clientes;
using AgendaSalon.Domain.Comun;
using static AgendaSalon.Domain.Tests.Datos;

namespace AgendaSalon.Domain.Tests;

public class CitaTests
{
    private static Cita ReservarCorteALas10()
    {
        var corte = Corte();
        return Cita.Reservar(CodigoCita.Generar(), Maria(), corte, EstilistaDeMartes(corte), ElMartesA(10), null, Ahora);
    }

    [Fact]
    public void Reservar_CalculaElFinYGuardaElPrecioDelMomento()
    {
        var cita = ReservarCorteALas10();

        Assert.Equal(ElMartesA(11), cita.Fin);
        Assert.Equal(500m, cita.PrecioAcordado);
        Assert.Equal(EstadoCita.Confirmada, cita.Estado);
    }

    [Fact]
    public void CambiarElPrecioDelServicio_NoAfectaCitasYaReservadas()
    {
        var cita = ReservarCorteALas10();

        cita.Servicio.CambiarPrecio(650m);

        Assert.Equal(500m, cita.PrecioAcordado);
    }

    [Fact]
    public void Reservar_ConEstilistaQueNoHaceElServicio_SeRechaza()
    {
        var corte = Corte();
        var estilistaSinServicios = EstilistaDeMartes();

        Assert.Throws<ReglaNegocioException>(() =>
            Cita.Reservar(CodigoCita.Generar(), Maria(), corte, estilistaSinServicios, ElMartesA(10), null, Ahora));
    }

    [Fact]
    public void Reservar_EnElPasado_SeRechaza()
    {
        var corte = Corte();

        Assert.Throws<ReglaNegocioException>(() =>
            Cita.Reservar(CodigoCita.Generar(), Maria(), corte, EstilistaDeMartes(corte), Ahora.AddHours(-1), null, Ahora));
    }

    [Fact]
    public void Reservar_ConNotasDemasiadoLargas_SeRechaza()
    {
        var corte = Corte();
        var notas = new string('x', Cita.LongitudMaximaNotas + 1);

        Assert.Throws<ReglaNegocioException>(() =>
            Cita.Reservar(CodigoCita.Generar(), Maria(), corte, EstilistaDeMartes(corte), ElMartesA(10), notas, Ahora));
    }

    [Fact]
    public void Cancelar_ConElTelefonoDelCliente_DejaLaCitaCancelada()
    {
        var cita = ReservarCorteALas10();

        cita.Cancelar(Telefono.Crear("+1 809 555 1234"), Ahora);

        Assert.Equal(EstadoCita.Cancelada, cita.Estado);
    }

    [Fact]
    public void Cancelar_ConOtroTelefono_SeRechaza()
    {
        var cita = ReservarCorteALas10();

        Assert.Throws<ReglaNegocioException>(() => cita.Cancelar(Telefono.Crear("829-000-0000"), Ahora));
    }

    [Fact]
    public void Cancelar_DosVeces_SeRechaza()
    {
        var cita = ReservarCorteALas10();
        var telefono = cita.Cliente.Telefono;
        cita.Cancelar(telefono, Ahora);

        Assert.Throws<ReglaNegocioException>(() => cita.Cancelar(telefono, Ahora));
    }

    [Fact]
    public void Cancelar_DespuesDeEmpezar_SeRechaza()
    {
        var cita = ReservarCorteALas10();

        Assert.Throws<ReglaNegocioException>(() => cita.Cancelar(cita.Cliente.Telefono, ElMartesA(10, 5)));
    }

    [Fact]
    public void MarcarCompletada_AntesDeEmpezar_SeRechaza()
    {
        var cita = ReservarCorteALas10();

        Assert.Throws<ReglaNegocioException>(() => cita.MarcarCompletada(Ahora));
    }

    [Fact]
    public void MarcarCompletada_DespuesDeEmpezar_CierraLaCita()
    {
        var cita = ReservarCorteALas10();

        cita.MarcarCompletada(ElMartesA(11));

        Assert.Equal(EstadoCita.Completada, cita.Estado);
    }
}

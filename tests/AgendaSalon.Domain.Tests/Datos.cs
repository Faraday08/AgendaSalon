using AgendaSalon.Domain.Agenda;
using AgendaSalon.Domain.Clientes;
using AgendaSalon.Domain.Estilistas;
using AgendaSalon.Domain.Servicios;

namespace AgendaSalon.Domain.Tests;

/// <summary>Datos de prueba compartidos. El "ahora" es el lunes 2 de noviembre de 2026 a las 8:00.</summary>
internal static class Datos
{
    public static readonly DateTime Ahora = new(2026, 11, 2, 8, 0, 0);
    public static readonly DateOnly Martes = new(2026, 11, 3);

    public static readonly ReglasAgenda Reglas = new()
    {
        IntervaloMinutos = 30,
        AnticipacionMinimaMinutos = 60,
        DiasMaximosAdelanto = 30
    };

    public static DateTime ElMartesA(int hora, int minuto = 0) => Martes.ToDateTime(new TimeOnly(hora, minuto));

    public static Servicio Corte(int duracionMinutos = 60, decimal precio = 500m) =>
        Servicio.Crear("Corte", "Corte de prueba", CategoriaServicio.Crear("Corte"), duracionMinutos, precio);

    /// <summary>Estilista que trabaja los martes de 9:00 a 12:00 y realiza los servicios indicados.</summary>
    public static Estilista EstilistaDeMartes(params Servicio[] servicios)
    {
        var estilista = Estilista.Contratar("Ana", "Cortes");
        estilista.AgregarJornada(DayOfWeek.Tuesday, new TimeOnly(9, 0), new TimeOnly(12, 0));
        foreach (var servicio in servicios)
            estilista.AsignarServicio(servicio);
        return estilista;
    }

    public static Cliente Maria() => Cliente.Registrar("María Rodríguez", Telefono.Crear("809-555-1234"));
}

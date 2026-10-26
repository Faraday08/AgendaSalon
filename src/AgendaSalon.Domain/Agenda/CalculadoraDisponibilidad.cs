using AgendaSalon.Domain.Estilistas;

namespace AgendaSalon.Domain.Agenda;

/// <summary>Servicio de dominio que calcula las horas de inicio libres a partir de las jornadas y las citas ocupadas.</summary>
public static class CalculadoraDisponibilidad
{
    public static IReadOnlyList<DateTime> HorasLibres(
        DateOnly fecha,
        int duracionMinutos,
        IEnumerable<HorarioLaboral> jornadas,
        IEnumerable<Intervalo> ocupados,
        DateTime ahora,
        ReglasAgenda reglas)
    {
        ArgumentNullException.ThrowIfNull(jornadas);
        ArgumentNullException.ThrowIfNull(ocupados);
        ArgumentNullException.ThrowIfNull(reglas);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(duracionMinutos);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(reglas.IntervaloMinutos);

        var hoy = DateOnly.FromDateTime(ahora);
        if (fecha < hoy || fecha > hoy.AddDays(reglas.DiasMaximosAdelanto))
            return [];

        var primeraHoraPermitida = ahora.AddMinutes(reglas.AnticipacionMinimaMinutos);
        var duracion = TimeSpan.FromMinutes(duracionMinutos);
        var paso = TimeSpan.FromMinutes(reglas.IntervaloMinutos);
        var citasDelDia = ocupados.ToList();
        var libres = new List<DateTime>();

        foreach (var jornada in jornadas.Where(j => j.Dia == fecha.DayOfWeek).OrderBy(j => j.Inicio))
        {
            var finJornada = fecha.ToDateTime(jornada.Fin);
            for (var inicio = fecha.ToDateTime(jornada.Inicio); inicio + duracion <= finJornada; inicio += paso)
            {
                if (inicio < primeraHoraPermitida)
                    continue;

                var propuesta = new Intervalo(inicio, inicio + duracion);
                if (citasDelDia.Any(c => c.SeSolapaCon(propuesta)))
                    continue;

                libres.Add(inicio);
            }
        }

        return libres;
    }
}

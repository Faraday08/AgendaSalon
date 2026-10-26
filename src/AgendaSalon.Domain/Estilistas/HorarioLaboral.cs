using AgendaSalon.Domain.Comun;

namespace AgendaSalon.Domain.Estilistas;

/// <summary>Una jornada semanal de un estilista. Puede haber varias por día (por ejemplo, mañana y tarde).</summary>
public sealed class HorarioLaboral : Entidad
{
    private HorarioLaboral() { }

    public int EstilistaId { get; private set; }
    public DayOfWeek Dia { get; private set; }
    public TimeOnly Inicio { get; private set; }
    public TimeOnly Fin { get; private set; }

    internal static HorarioLaboral Crear(DayOfWeek dia, TimeOnly inicio, TimeOnly fin) =>
        inicio < fin
            ? new HorarioLaboral { Dia = dia, Inicio = inicio, Fin = fin }
            : throw new ReglaNegocioException("La jornada debe terminar después de empezar.");

    internal bool SeSolapaCon(DayOfWeek dia, TimeOnly inicio, TimeOnly fin) =>
        Dia == dia && Inicio < fin && inicio < Fin;
}

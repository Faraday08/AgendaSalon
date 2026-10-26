namespace AgendaSalon.Domain.Agenda;

/// <summary>Rango de tiempo semiabierto [Inicio, Fin): una cita que termina a las 10:00 no choca con otra que empieza a las 10:00.</summary>
public sealed record Intervalo(DateTime Inicio, DateTime Fin)
{
    public bool SeSolapaCon(Intervalo otro)
    {
        ArgumentNullException.ThrowIfNull(otro);
        return Inicio < otro.Fin && otro.Inicio < Fin;
    }
}

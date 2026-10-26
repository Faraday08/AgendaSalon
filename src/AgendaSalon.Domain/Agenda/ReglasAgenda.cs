namespace AgendaSalon.Domain.Agenda;

/// <summary>Políticas de reserva del salón (sección "Agenda" de appsettings.json).</summary>
public sealed class ReglasAgenda
{
    /// <summary>Cada cuántos minutos se ofrece una hora de inicio (9:00, 9:15, 9:30...).</summary>
    public int IntervaloMinutos { get; set; } = 15;

    /// <summary>Tiempo mínimo entre el momento de reservar y el inicio de la cita.</summary>
    public int AnticipacionMinimaMinutos { get; set; } = 60;

    /// <summary>Hasta cuántos días en el futuro se puede reservar.</summary>
    public int DiasMaximosAdelanto { get; set; } = 30;
}

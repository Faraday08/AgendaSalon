using AgendaSalon.Domain.Agenda;
using AgendaSalon.Domain.Clientes;
using AgendaSalon.Domain.Comun;
using AgendaSalon.Domain.Estilistas;
using AgendaSalon.Domain.Servicios;

namespace AgendaSalon.Domain.Citas;

public sealed class Cita : Entidad
{
    public const int LongitudMaximaNotas = 300;

    private Cita() { }

    public CodigoCita Codigo { get; private set; } = null!;
    public int ClienteId { get; private set; }
    public Cliente Cliente { get; private set; } = null!;
    public int ServicioId { get; private set; }
    public Servicio Servicio { get; private set; } = null!;
    public int EstilistaId { get; private set; }
    public Estilista Estilista { get; private set; } = null!;

    /// <summary>Hora local del salón.</summary>
    public DateTime Inicio { get; private set; }

    /// <summary>
    /// Se guarda aunque se pueda calcular con la duración del servicio: si la duración cambia después,
    /// la cita debe seguir ocupando el tiempo que se reservó.
    /// </summary>
    public DateTime Fin { get; private set; }

    /// <summary>Precio del servicio al momento de reservar. Si luego cambia el precio, la cita conserva este.</summary>
    public decimal PrecioAcordado { get; private set; }

    public EstadoCita Estado { get; private set; }
    public string? Notas { get; private set; }
    public DateTime CreadaEn { get; private set; }

    public Intervalo Horario => new(Inicio, Fin);

    public static Cita Reservar(
        CodigoCita codigo, Cliente cliente, Servicio servicio, Estilista estilista, DateTime inicio, string? notas, DateTime ahora)
    {
        ArgumentNullException.ThrowIfNull(codigo);
        ArgumentNullException.ThrowIfNull(cliente);
        ArgumentNullException.ThrowIfNull(servicio);
        ArgumentNullException.ThrowIfNull(estilista);

        if (!servicio.Activo)
            throw new ReglaNegocioException("El servicio ya no se ofrece.");
        if (!estilista.Activo || !estilista.RealizaServicio(servicio))
            throw new ReglaNegocioException($"{estilista.Nombre} no realiza el servicio {servicio.Nombre}.");
        if (inicio <= ahora)
            throw new ReglaNegocioException("No se puede reservar una cita en el pasado.");

        return new Cita
        {
            Codigo = codigo,
            Cliente = cliente,
            Servicio = servicio,
            Estilista = estilista,
            Inicio = inicio,
            Fin = inicio.AddMinutes(servicio.DuracionMinutos),
            PrecioAcordado = servicio.Precio,
            Estado = EstadoCita.Confirmada,
            Notas = Texto.Opcional(notas, LongitudMaximaNotas, "las notas"),
            CreadaEn = ahora
        };
    }

    /// <summary>El cliente cancela. Se pide su teléfono para confirmar que la cita es suya.</summary>
    public void Cancelar(Telefono telefonoDelSolicitante, DateTime ahora)
    {
        if (Cliente.Telefono != telefonoDelSolicitante)
            throw new ReglaNegocioException("El teléfono no coincide con el de la cita.");
        AsegurarQueSigueConfirmada();
        if (Inicio <= ahora)
            throw new ReglaNegocioException("No se puede cancelar una cita que ya empezó.");

        Estado = EstadoCita.Cancelada;
    }

    public void MarcarCompletada(DateTime ahora) => CerrarDespuesDeEmpezar(EstadoCita.Completada, ahora);

    public void MarcarNoAsistio(DateTime ahora) => CerrarDespuesDeEmpezar(EstadoCita.NoAsistio, ahora);

    private void CerrarDespuesDeEmpezar(EstadoCita estadoFinal, DateTime ahora)
    {
        AsegurarQueSigueConfirmada();
        if (Inicio > ahora)
            throw new ReglaNegocioException("La cita todavía no ha empezado.");

        Estado = estadoFinal;
    }

    private void AsegurarQueSigueConfirmada()
    {
        if (Estado != EstadoCita.Confirmada)
            throw new ReglaNegocioException("La cita ya no está confirmada.");
    }
}

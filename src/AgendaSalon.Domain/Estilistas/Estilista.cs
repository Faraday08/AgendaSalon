using AgendaSalon.Domain.Agenda;
using AgendaSalon.Domain.Comun;
using AgendaSalon.Domain.Servicios;

namespace AgendaSalon.Domain.Estilistas;

public sealed class Estilista : Entidad
{
    public const int LongitudMaximaNombre = 80;
    public const int LongitudMaximaEspecialidad = 80;

    private readonly List<Servicio> _servicios = [];
    private readonly List<HorarioLaboral> _horarios = [];

    private Estilista() { }

    public string Nombre { get; private set; } = null!;
    public string Especialidad { get; private set; } = null!;
    public bool Activo { get; private set; } = true;

    public IReadOnlyCollection<Servicio> Servicios => _servicios.AsReadOnly();
    public IReadOnlyCollection<HorarioLaboral> Horarios => _horarios.AsReadOnly();

    public static Estilista Contratar(string nombre, string especialidad) => new()
    {
        Nombre = Texto.Requerido(nombre, LongitudMaximaNombre, "el nombre del estilista"),
        Especialidad = Texto.Requerido(especialidad, LongitudMaximaEspecialidad, "la especialidad")
    };

    public void AsignarServicio(Servicio servicio)
    {
        ArgumentNullException.ThrowIfNull(servicio);
        if (!RealizaServicio(servicio))
            _servicios.Add(servicio);
    }

    public void AgregarJornada(DayOfWeek dia, TimeOnly inicio, TimeOnly fin)
    {
        if (_horarios.Any(h => h.SeSolapaCon(dia, inicio, fin)))
            throw new ReglaNegocioException("La jornada se cruza con otra del mismo día.");

        _horarios.Add(HorarioLaboral.Crear(dia, inicio, fin));
    }

    public bool RealizaServicio(Servicio servicio) =>
        _servicios.Contains(servicio) || (servicio.Id != 0 && _servicios.Any(s => s.Id == servicio.Id));

    public void Desactivar() => Activo = false;

    /// <summary>Horas en que este estilista puede empezar el servicio en la fecha indicada.</summary>
    public IReadOnlyList<DateTime> HorasLibres(
        Servicio servicio, DateOnly fecha, IEnumerable<Intervalo> ocupados, DateTime ahora, ReglasAgenda reglas)
    {
        ArgumentNullException.ThrowIfNull(servicio);
        return RealizaServicio(servicio)
            ? CalculadoraDisponibilidad.HorasLibres(fecha, servicio.DuracionMinutos, _horarios, ocupados, ahora, reglas)
            : [];
    }
}

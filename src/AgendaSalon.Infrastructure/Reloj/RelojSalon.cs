using AgendaSalon.Application.Abstracciones;
using Microsoft.Extensions.Options;

namespace AgendaSalon.Infrastructure.Reloj;

public sealed class OpcionesReloj
{
    /// <summary>Zona horaria IANA del salón.</summary>
    public string ZonaHoraria { get; set; } = "America/Santo_Domingo";
}

internal sealed class RelojSalon(TimeProvider tiempo, IOptions<OpcionesReloj> opciones) : IRelojSalon
{
    private readonly TimeZoneInfo _zona = TimeZoneInfo.FindSystemTimeZoneById(opciones.Value.ZonaHoraria);

    public DateTime Ahora => TimeZoneInfo.ConvertTime(tiempo.GetUtcNow(), _zona).DateTime;
}

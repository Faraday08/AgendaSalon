using AgendaSalon.Application.Comun;
using AgendaSalon.Domain.Servicios;

namespace AgendaSalon.Application.Disponibilidad;

public sealed record HuecoDisponibleDto(int EstilistaId, string EstilistaNombre, DateTime Inicio, DateTime Fin);

public interface IDisponibilidadService
{
    /// <summary>Horas libres para un servicio en una fecha (hora local del salón).</summary>
    Task<IReadOnlyList<HuecoDisponibleDto>> ConsultarAsync(
        int servicioId, DateOnly fecha, int? estilistaId, CancellationToken ct = default);
}

internal sealed class DisponibilidadService(IServicioRepositorio servicios, PlanificadorAgenda planificador)
    : IDisponibilidadService
{
    public async Task<IReadOnlyList<HuecoDisponibleDto>> ConsultarAsync(
        int servicioId, DateOnly fecha, int? estilistaId, CancellationToken ct = default)
    {
        var servicio = await servicios.ObtenerActivoAsync(servicioId, ct)
            ?? throw new NoEncontradoException(Mensajes.ServicioNoDisponible);

        var huecos = await planificador.BuscarHuecosAsync(servicio, fecha, estilistaId, ct);
        return huecos
            .Select(h => new HuecoDisponibleDto(h.Estilista.Id, h.Estilista.Nombre, h.Inicio, h.Inicio.AddMinutes(servicio.DuracionMinutos)))
            .ToList();
    }
}

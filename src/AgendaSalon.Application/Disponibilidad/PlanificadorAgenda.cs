using AgendaSalon.Application.Abstracciones;
using AgendaSalon.Domain.Agenda;
using AgendaSalon.Domain.Citas;
using AgendaSalon.Domain.Comun;
using AgendaSalon.Domain.Estilistas;
using AgendaSalon.Domain.Servicios;
using Microsoft.Extensions.Options;

namespace AgendaSalon.Application.Disponibilidad;

internal sealed record HuecoLibre(Estilista Estilista, DateTime Inicio);

/// <summary>Busca los huecos libres de un servicio. Lo comparten la consulta de disponibilidad y la reserva.</summary>
internal sealed class PlanificadorAgenda(
    IEstilistaRepositorio estilistas,
    ICitaRepositorio citas,
    IRelojSalon reloj,
    IOptions<ReglasAgenda> reglas)
{
    /// <returns>Huecos ordenados por hora y luego por nombre del estilista.</returns>
    public async Task<IReadOnlyList<HuecoLibre>> BuscarHuecosAsync(
        Servicio servicio, DateOnly fecha, int? estilistaId, CancellationToken ct)
    {
        var candidatos = await estilistas.ListarActivosAsync(servicio.Id, ct);
        if (estilistaId is not null)
        {
            candidatos = candidatos.Where(e => e.Id == estilistaId).ToList();
            if (candidatos.Count == 0)
                throw new ReglaNegocioException("Ese estilista no realiza el servicio elegido.");
        }

        var ocupacion = await citas.ObtenerOcupacionAsync(candidatos.Select(e => e.Id).ToList(), fecha, ct);
        var ahora = reloj.Ahora;

        return candidatos
            .SelectMany(e => e.HorasLibres(servicio, fecha, ocupacion[e.Id], ahora, reglas.Value)
                .Select(inicio => new HuecoLibre(e, inicio)))
            .OrderBy(h => h.Inicio)
            .ThenBy(h => h.Estilista.Nombre, StringComparer.CurrentCulture)
            .ToList();
    }
}

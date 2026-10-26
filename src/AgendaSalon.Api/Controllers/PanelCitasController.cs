using AgendaSalon.Api.Seguridad;
using AgendaSalon.Application.Panel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgendaSalon.Api.Controllers;

/// <summary>Agenda del salón para el personal. Los cambios se avisan en tiempo real por el hub /hubs/agenda.</summary>
[ApiController]
[Route("api/panel/citas")]
[Authorize(Policy = PoliticasAutorizacion.Personal)]
public sealed class PanelCitasController(IPanelCitasService panel) : ControllerBase
{
    /// <summary>Citas entre dos fechas, ambas incluidas. Un estilista solo recibe las suyas.</summary>
    [HttpGet]
    public Task<IReadOnlyList<CitaPanelDto>> Listar(
        [FromQuery] DateOnly desde, [FromQuery] DateOnly hasta, [FromQuery] int? estilistaId, CancellationToken ct) =>
        panel.ListarAsync(desde, hasta, estilistaId, ct);

    [HttpPost("{codigo}/completada")]
    public Task<CitaPanelDto> MarcarCompletada(string codigo, CancellationToken ct) =>
        panel.MarcarCompletadaAsync(codigo, ct);

    [HttpPost("{codigo}/no-asistio")]
    public Task<CitaPanelDto> MarcarNoAsistio(string codigo, CancellationToken ct) =>
        panel.MarcarNoAsistioAsync(codigo, ct);

    /// <summary>Cancelación hecha por el salón (no pide el teléfono del cliente).</summary>
    [HttpPost("{codigo}/cancelacion")]
    public Task<CitaPanelDto> Cancelar(string codigo, CancellationToken ct) =>
        panel.CancelarAsync(codigo, ct);
}

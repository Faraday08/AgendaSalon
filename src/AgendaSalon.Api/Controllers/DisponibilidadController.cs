using AgendaSalon.Application.Disponibilidad;
using Microsoft.AspNetCore.Mvc;

namespace AgendaSalon.Api.Controllers;

[ApiController]
[Route("api/disponibilidad")]
public sealed class DisponibilidadController(IDisponibilidadService disponibilidad) : ControllerBase
{
    /// <summary>Horas libres para un servicio en una fecha (hora local del salón).</summary>
    [HttpGet]
    public Task<IReadOnlyList<HuecoDisponibleDto>> Consultar(
        [FromQuery] int servicioId, [FromQuery] DateOnly fecha, [FromQuery] int? estilistaId, CancellationToken ct) =>
        disponibilidad.ConsultarAsync(servicioId, fecha, estilistaId, ct);
}

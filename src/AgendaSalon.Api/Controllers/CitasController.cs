using AgendaSalon.Application.Reservas;
using Microsoft.AspNetCore.Mvc;

namespace AgendaSalon.Api.Controllers;

[ApiController]
[Route("api/citas")]
public sealed class CitasController(IReservaService reservas) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<CitaDto>> Crear(CrearCitaRequest solicitud, CancellationToken ct)
    {
        var cita = await reservas.CrearAsync(solicitud, ct);
        return CreatedAtAction(nameof(Obtener), new { codigo = cita.Codigo }, cita);
    }

    [HttpGet("{codigo}")]
    public Task<CitaDto> Obtener(string codigo, CancellationToken ct) =>
        reservas.ObtenerAsync(codigo, ct);

    /// <summary>Cancela la cita. Pide el teléfono con que se reservó para confirmar que es del cliente.</summary>
    [HttpPost("{codigo}/cancelacion")]
    public Task<CitaDto> Cancelar(string codigo, CancelarCitaRequest solicitud, CancellationToken ct) =>
        reservas.CancelarAsync(codigo, solicitud.Telefono, ct);
}

using AgendaSalon.Application.Catalogo;
using Microsoft.AspNetCore.Mvc;

namespace AgendaSalon.Api.Controllers;

[ApiController]
[Route("api")]
public sealed class CatalogoController(ICatalogoService catalogo) : ControllerBase
{
    /// <summary>Servicios que ofrece el salón, con duración y precio.</summary>
    [HttpGet("servicios")]
    public Task<IReadOnlyList<ServicioDto>> Servicios(CancellationToken ct) =>
        catalogo.ListarServiciosAsync(ct);

    /// <summary>Estilistas activos. Con servicioId, solo los que realizan ese servicio.</summary>
    [HttpGet("estilistas")]
    public Task<IReadOnlyList<EstilistaDto>> Estilistas([FromQuery] int? servicioId, CancellationToken ct) =>
        catalogo.ListarEstilistasAsync(servicioId, ct);
}

using AgendaSalon.Application.Orientacion;
using Microsoft.AspNetCore.Mvc;

namespace AgendaSalon.Api.Controllers;

public sealed record InterpretarRequest(string Texto);

/// <summary>Ayuda al cliente a elegir el servicio: guía de preguntas y lectura de texto libre por palabras clave.</summary>
[ApiController]
[Route("api/orientacion")]
public sealed class OrientacionController(IOrientacionService orientacion) : ControllerBase
{
    /// <summary>Todas las preguntas con sus opciones y recomendaciones, ya con los precios actuales.</summary>
    [HttpGet("guia")]
    public Task<GuiaOrientacionDto> Guia(CancellationToken ct) => orientacion.ObtenerGuiaAsync(ct);

    /// <summary>Lee lo que escribió el cliente ("me caso el sábado") y sugiere por dónde seguir.</summary>
    [HttpPost("interpretar")]
    public Task<InterpretacionDto> Interpretar(InterpretarRequest solicitud, CancellationToken ct) =>
        orientacion.InterpretarAsync(solicitud.Texto, ct);
}

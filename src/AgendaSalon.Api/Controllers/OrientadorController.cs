using AgendaSalon.Api.Seguridad;
using AgendaSalon.Application.Orientador;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AgendaSalon.Api.Controllers;

public sealed record EstadoOrientadorDto(bool Disponible);

/// <summary>Chat público con el asistente orientador.</summary>
[ApiController]
[Route("api/orientador")]
public sealed class OrientadorController(IOrientador orientador) : ControllerBase
{
    /// <summary>Indica si el asistente está activo, para que la web muestre u oculte el chat.</summary>
    [HttpGet("estado")]
    public EstadoOrientadorDto Estado() => new(orientador.Disponible);

    /// <summary>Envía un mensaje y devuelve la respuesta. Responde 503 si el asistente no está configurado.</summary>
    [HttpPost("mensajes")]
    [EnableRateLimiting(PoliticasAutorizacion.LimiteOrientador)]
    public Task<RespuestaOrientadorDto> Conversar(MensajeOrientadorRequest solicitud, CancellationToken ct) =>
        orientador.ConversarAsync(solicitud, ct);
}

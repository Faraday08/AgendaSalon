using AgendaSalon.Api.Seguridad;
using AgendaSalon.Application.Autenticacion;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AgendaSalon.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(IAutenticacionService autenticacion) : ControllerBase
{
    /// <summary>Inicia sesión en el panel y devuelve un token JWT. Limitado a pocos intentos por minuto.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(PoliticasAutorizacion.LimiteLogin)]
    public Task<SesionDto> IniciarSesion(IniciarSesionRequest solicitud, CancellationToken ct) =>
        autenticacion.IniciarSesionAsync(solicitud, ct);
}

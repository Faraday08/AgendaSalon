using AgendaSalon.Api.Seguridad;
using AgendaSalon.Application.Usuarios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgendaSalon.Api.Controllers;

[ApiController]
[Route("api/panel/usuarios")]
[Authorize(Policy = PoliticasAutorizacion.Administrador)]
public sealed class UsuariosController(IUsuarioService usuarios) : ControllerBase
{
    /// <summary>Crea la cuenta con la que un estilista entra al panel a ver su agenda.</summary>
    [HttpPost("estilistas")]
    public async Task<ActionResult<UsuarioDto>> CrearCuentaEstilista(CrearCuentaEstilistaRequest solicitud, CancellationToken ct)
    {
        var usuario = await usuarios.CrearCuentaEstilistaAsync(solicitud, ct);
        return StatusCode(StatusCodes.Status201Created, usuario);
    }
}

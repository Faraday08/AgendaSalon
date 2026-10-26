using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Security.Claims;
using System.Text;
using AgendaSalon.Application.Abstracciones;
using AgendaSalon.Domain.Usuarios;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace AgendaSalon.Infrastructure.Seguridad;

/// <summary>Sección "Jwt" de la configuración. La clave nunca va en appsettings.json: se guarda en user-secrets o en variables de entorno.</summary>
public sealed class OpcionesJwt
{
    public const int BytesMinimosClave = 32;

    [Required]
    public string Emisor { get; set; } = "";

    [Required]
    public string Audiencia { get; set; } = "";

    [Required]
    public string Clave { get; set; } = "";

    [Range(5, 24 * 60)]
    public int MinutosExpiracion { get; set; } = 60;

    public SymmetricSecurityKey CrearClaveFirma() => new(Encoding.UTF8.GetBytes(Clave));
}

/// <summary>Nombres de los claims que viajan en el token y que lee la API.</summary>
public static class ClaimsAgenda
{
    public const string Id = JwtRegisteredClaimNames.Sub;
    public const string Nombre = "name";
    public const string Rol = "role";
    public const string EstilistaId = "estilista_id";
}

internal sealed class GeneradorTokenJwt(IOptions<OpcionesJwt> opciones, TimeProvider tiempo) : IGeneradorToken
{
    private readonly JsonWebTokenHandler _manejador = new();

    public TokenAcceso Generar(Usuario usuario)
    {
        ArgumentNullException.ThrowIfNull(usuario);
        var config = opciones.Value;

        var claims = new List<Claim>
        {
            new(ClaimsAgenda.Id, usuario.Id.ToString(CultureInfo.InvariantCulture)),
            new(ClaimsAgenda.Nombre, usuario.Nombre),
            new(ClaimsAgenda.Rol, usuario.Rol.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        if (usuario.EstilistaId is int estilistaId)
            claims.Add(new Claim(ClaimsAgenda.EstilistaId, estilistaId.ToString(CultureInfo.InvariantCulture)));

        var ahora = tiempo.GetUtcNow().UtcDateTime;
        var expira = ahora.AddMinutes(config.MinutosExpiracion);
        var token = _manejador.CreateToken(new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Issuer = config.Emisor,
            Audience = config.Audiencia,
            IssuedAt = ahora,
            NotBefore = ahora,
            Expires = expira,
            SigningCredentials = new SigningCredentials(config.CrearClaveFirma(), SecurityAlgorithms.HmacSha256)
        });

        return new TokenAcceso(token, expira);
    }
}

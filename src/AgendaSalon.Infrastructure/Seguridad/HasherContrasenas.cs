using AgendaSalon.Application.Abstracciones;
using Microsoft.AspNetCore.Identity;

namespace AgendaSalon.Infrastructure.Seguridad;

/// <summary>
/// Usa el PasswordHasher de ASP.NET Core Identity (PBKDF2 con sal y versión en el propio hash),
/// en lugar de un algoritmo propio.
/// </summary>
internal sealed class HasherContrasenas : IHasherContrasenas
{
    // La implementación por defecto no usa el usuario; se pasa un objeto fijo.
    private static readonly object SinUsuario = new();
    private static readonly PasswordHasher<object> Hasher = new();
    private static readonly Lazy<string> HashFicticio = new(() => Hasher.HashPassword(SinUsuario, Guid.NewGuid().ToString()));

    public string Hashear(string contrasena)
    {
        ArgumentException.ThrowIfNullOrEmpty(contrasena);
        return Hasher.HashPassword(SinUsuario, contrasena);
    }

    public bool Verificar(string? hash, string contrasena)
    {
        var resultado = Hasher.VerifyHashedPassword(SinUsuario, hash ?? HashFicticio.Value, contrasena ?? "");
        return hash is not null && resultado is not PasswordVerificationResult.Failed;
    }
}

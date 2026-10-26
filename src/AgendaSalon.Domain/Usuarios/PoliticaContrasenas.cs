using AgendaSalon.Domain.Comun;

namespace AgendaSalon.Domain.Usuarios;

/// <summary>Requisitos mínimos de una contraseña nueva. Se valida antes de calcular el hash.</summary>
public static class PoliticaContrasenas
{
    public const int LongitudMinima = 10;
    public const int LongitudMaxima = 128;

    public static void Validar(string? contrasena)
    {
        if (contrasena is null || contrasena.Length < LongitudMinima)
            throw new ReglaNegocioException($"La contraseña debe tener al menos {LongitudMinima} caracteres.");
        if (contrasena.Length > LongitudMaxima)
            throw new ReglaNegocioException($"La contraseña no puede pasar de {LongitudMaxima} caracteres.");
        if (!contrasena.Any(char.IsLetter) || !contrasena.Any(char.IsDigit))
            throw new ReglaNegocioException("La contraseña debe combinar letras y números.");
    }
}

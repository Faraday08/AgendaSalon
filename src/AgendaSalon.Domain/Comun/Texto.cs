namespace AgendaSalon.Domain.Comun;

/// <summary>Validaciones de texto compartidas por las entidades.</summary>
internal static class Texto
{
    /// <param name="campo">Nombre del campo con su artículo, por ejemplo "el nombre" o "la descripción".</param>
    public static string Requerido(string? valor, int longitudMaxima, string campo)
    {
        var limpio = valor?.Trim() ?? "";
        if (limpio.Length == 0)
            throw new ReglaNegocioException($"Falta {campo}.");
        if (limpio.Length > longitudMaxima)
            throw new ReglaNegocioException($"{char.ToUpperInvariant(campo[0])}{campo[1..]} no puede pasar de {longitudMaxima} caracteres.");

        return limpio;
    }

    public static string? Opcional(string? valor, int longitudMaxima, string campo) =>
        string.IsNullOrWhiteSpace(valor) ? null : Requerido(valor, longitudMaxima, campo);
}

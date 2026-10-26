using System.Diagnostics.CodeAnalysis;
using System.Net.Mail;
using AgendaSalon.Domain.Comun;

namespace AgendaSalon.Domain.Usuarios;

/// <summary>Correo electrónico normalizado en minúsculas y sin espacios.</summary>
public sealed record Correo
{
    public const int LongitudMaxima = 254;

    private Correo(string valor) => Valor = valor;

    public string Valor { get; }

    public static Correo Crear(string? texto) =>
        TryCrear(texto, out var correo)
            ? correo
            : throw new ReglaNegocioException("Escribe un correo válido, por ejemplo nombre@dominio.com.");

    public static bool TryCrear(string? texto, [NotNullWhen(true)] out Correo? correo)
    {
        var normalizado = texto?.Trim().ToLowerInvariant() ?? "";
        var esValido = normalizado.Length <= LongitudMaxima
            && MailAddress.TryCreate(normalizado, out var direccion)
            && direccion.Address == normalizado
            && direccion.Host.Contains('.', StringComparison.Ordinal);

        correo = esValido ? new Correo(normalizado) : null;
        return esValido;
    }

    public override string ToString() => Valor;
}

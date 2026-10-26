using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;

namespace AgendaSalon.Domain.Citas;

/// <summary>Código corto que recibe el cliente para consultar o cancelar su cita. Evita caracteres confusos como O/0 o I/1.</summary>
public sealed record CodigoCita
{
    public const int Longitud = 6;
    private const string Alfabeto = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    private CodigoCita(string valor) => Valor = valor;

    public string Valor { get; }

    public static CodigoCita Generar() => new(RandomNumberGenerator.GetString(Alfabeto, Longitud));

    /// <summary>Acepta minúsculas y espacios alrededor. Devuelve false si el texto no tiene el formato de un código.</summary>
    public static bool TryCrear(string? texto, [NotNullWhen(true)] out CodigoCita? codigo)
    {
        var normalizado = texto?.Trim().ToUpperInvariant() ?? "";
        codigo = normalizado.Length == Longitud && normalizado.All(Alfabeto.Contains)
            ? new CodigoCita(normalizado)
            : null;
        return codigo is not null;
    }

    /// <summary>Reconstruye un código guardado. Lanza una excepción si no es válido.</summary>
    public static CodigoCita Desde(string valor) =>
        TryCrear(valor, out var codigo) ? codigo : throw new ArgumentException($"'{valor}' no es un código de cita válido.", nameof(valor));

    public override string ToString() => Valor;
}

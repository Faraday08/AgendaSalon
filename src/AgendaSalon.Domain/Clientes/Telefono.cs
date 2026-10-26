using AgendaSalon.Domain.Comun;

namespace AgendaSalon.Domain.Clientes;

/// <summary>Teléfono dominicano de 10 dígitos. Acepta formatos como 809-555-1234, (829) 555 1234 o +1 849 555 1234.</summary>
public sealed record Telefono
{
    public const int Digitos = 10;

    private Telefono(string valor) => Valor = valor;

    /// <summary>Solo los 10 dígitos, sin separadores.</summary>
    public string Valor { get; }

    public static Telefono Crear(string? texto)
    {
        var digitos = new string((texto ?? "").Where(char.IsAsciiDigit).ToArray());
        if (digitos.Length == Digitos + 1 && digitos[0] == '1')
            digitos = digitos[1..];

        return digitos.Length == Digitos
            ? new Telefono(digitos)
            : throw new ReglaNegocioException("El teléfono debe tener 10 dígitos, por ejemplo 809-555-1234.");
    }

    public override string ToString() => $"{Valor[..3]}-{Valor[3..6]}-{Valor[6..]}";
}

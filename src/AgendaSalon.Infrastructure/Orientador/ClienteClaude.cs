using Anthropic;
using Microsoft.Extensions.Options;

namespace AgendaSalon.Infrastructure.Orientador;

/// <summary>
/// Cliente del SDK de Anthropic, o null si no hay clave configurada. Se registra en DI por separado
/// para que las pruebas lo cambien por uno que apunta a una API simulada.
/// </summary>
public sealed class ClienteClaude(AnthropicClient? cliente)
{
    public AnthropicClient? Cliente { get; } = cliente;

    public static ClienteClaude DesdeOpciones(IOptions<OpcionesOrientador> opciones)
    {
        ArgumentNullException.ThrowIfNull(opciones);
        var clave = opciones.Value.ClaveApi ?? Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");
        return new ClienteClaude(string.IsNullOrWhiteSpace(clave)
            ? null
            : new AnthropicClient { ApiKey = clave, BaseUrl = opciones.Value.UrlApi });
    }
}

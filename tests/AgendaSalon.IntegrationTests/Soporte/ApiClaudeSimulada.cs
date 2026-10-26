using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace AgendaSalon.IntegrationTests.Soporte;

/// <summary>
/// Sustituye a la API de Claude: responde con las respuestas preparadas, en orden, y guarda cada petición
/// para revisar qué envió el SDK. Permite probar el ciclo de herramientas sin red ni costo.
/// </summary>
internal sealed class ApiClaudeSimulada : HttpMessageHandler
{
    private readonly Queue<string> _respuestas = new();

    public List<JsonNode> Peticiones { get; } = [];
    public List<HttpRequestMessage> Encabezados { get; } = [];

    /// <summary>Claude pide ejecutar una herramienta. Incluye un bloque de razonamiento, que debe volver intacto.</summary>
    public ApiClaudeSimulada PideHerramienta(string id, string nombre, object entrada, string firma = "firma-de-prueba") =>
        Encolar("tool_use",
            new { type = "thinking", thinking = "", signature = firma },
            new { type = "tool_use", id, name = nombre, input = entrada });

    public ApiClaudeSimulada Responde(string texto) => Encolar("end_turn", new { type = "text", text = texto });

    public ApiClaudeSimulada Rechaza() => Encolar("refusal");

    private ApiClaudeSimulada Encolar(string motivo, params object[] contenido)
    {
        _respuestas.Enqueue(JsonSerializer.Serialize(new
        {
            id = $"msg_{_respuestas.Count + 1}",
            type = "message",
            role = "assistant",
            model = "claude-opus-5-5",
            content = contenido,
            stop_reason = motivo,
            stop_sequence = (string?)null,
            usage = new { input_tokens = 100, output_tokens = 20 }
        }));
        return this;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var cuerpo = request.Content is null ? "{}" : await request.Content.ReadAsStringAsync(cancellationToken);
        Peticiones.Add(JsonNode.Parse(cuerpo)!);
        Encabezados.Add(request);

        if (!_respuestas.TryDequeue(out var respuesta))
            throw new InvalidOperationException("La prueba no preparó más respuestas de Claude.");

        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(respuesta, Encoding.UTF8, "application/json")
        };
    }
}

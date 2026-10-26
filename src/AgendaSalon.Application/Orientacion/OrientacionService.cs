using System.Globalization;
using System.Text;
using AgendaSalon.Application.Catalogo;
using AgendaSalon.Domain.Comun;

namespace AgendaSalon.Application.Orientacion;

public sealed record RecomendacionDto(string Motivo, IReadOnlyList<ServicioDto> Servicios);

/// <summary>Una opción lleva a <see cref="SiguientePregunta"/> o termina en <see cref="Recomendacion"/>.</summary>
public sealed record OpcionGuiaDto(string Texto, string? SiguientePregunta, RecomendacionDto? Recomendacion);

public sealed record PreguntaGuiaDto(string Id, string Texto, IReadOnlyList<OpcionGuiaDto> Opciones);

public sealed record GuiaOrientacionDto(string PreguntaInicial, IReadOnlyList<PreguntaGuiaDto> Preguntas);

/// <summary>Resultado de leer lo que escribió el cliente: una pregunta para seguir, una recomendación o nada.</summary>
public sealed record InterpretacionDto(bool Entendido, string Mensaje, string? Pregunta, RecomendacionDto? Recomendacion);

/// <summary>
/// Orienta al cliente que no sabe qué servicio pedir, sin IA: una guía de preguntas con opciones
/// y unas reglas de palabras clave para cuando escribe con sus palabras.
/// </summary>
public interface IOrientacionService
{
    Task<GuiaOrientacionDto> ObtenerGuiaAsync(CancellationToken ct = default);

    Task<InterpretacionDto> InterpretarAsync(string texto, CancellationToken ct = default);
}

internal sealed class OrientacionService(ICatalogoService catalogo) : IOrientacionService
{
    public const int LongitudMaximaTexto = 300;

    public async Task<GuiaOrientacionDto> ObtenerGuiaAsync(CancellationToken ct = default)
    {
        var servicios = await ServiciosPorNombreAsync(ct);
        var preguntas = ReglasOrientacion.Preguntas
            .Select(p => new PreguntaGuiaDto(
                p.Id,
                p.Texto,
                p.Opciones
                    .Select(o => new OpcionGuiaDto(o.Texto, o.Siguiente, Resolver(o.Recomendacion, servicios)))
                    // Una opción cuyo servicio ya no se ofrece no lleva a ningún lado: se oculta.
                    .Where(o => o.SiguientePregunta is not null || o.Recomendacion is not null)
                    .ToList()))
            .ToList();

        return new GuiaOrientacionDto(ReglasOrientacion.Inicio, preguntas);
    }

    public async Task<InterpretacionDto> InterpretarAsync(string texto, CancellationToken ct = default)
    {
        var limpio = texto?.Trim() ?? "";
        if (limpio.Length == 0)
            throw new ReglaNegocioException("Escribe qué te quieres hacer.");
        if (limpio.Length > LongitudMaximaTexto)
            throw new ReglaNegocioException($"Escríbelo en menos de {LongitudMaximaTexto} caracteres.");

        var palabras = Palabras(limpio);
        var regla = ReglasOrientacion.Palabras
            .Select((r, orden) => (Regla: r, Orden: orden, Puntos: Puntos(r, palabras)))
            .Where(x => x.Puntos > 0)
            .OrderByDescending(x => x.Puntos)
            .ThenBy(x => x.Orden)
            .Select(x => x.Regla)
            .FirstOrDefault();

        if (regla?.Pregunta is not null)
            return new InterpretacionDto(true, "Te entiendo. Una pregunta más:", regla.Pregunta, null);

        var recomendacion = Resolver(regla?.Recomendacion, await ServiciosPorNombreAsync(ct));
        return recomendacion is not null
            ? new InterpretacionDto(true, "Esto es lo que te recomiendo:", null, recomendacion)
            : new InterpretacionDto(false, "No estoy seguro de qué buscas. Elige una de estas opciones:", ReglasOrientacion.Inicio, null);
    }

    /// <summary>Grupos cumplidos, o 0 si falta alguno: la regla vale solo si todos sus grupos aparecen.</summary>
    private static int Puntos(ReglaPalabras regla, IReadOnlyList<string> palabras)
    {
        var cumplidos = regla.Grupos.Count(grupo => grupo.Any(raiz => palabras.Any(p => p.StartsWith(raiz, StringComparison.Ordinal))));
        return cumplidos == regla.Grupos.Length ? cumplidos : 0;
    }

    /// <summary>Minúsculas, sin tildes ni signos: "¡Me CASO el sábado!" → [me, caso, el, sabado].</summary>
    internal static IReadOnlyList<string> Palabras(string texto)
    {
        var sinTildes = new StringBuilder(texto.Length);
        foreach (var letra in texto.ToLowerInvariant().Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(letra) != UnicodeCategory.NonSpacingMark)
                sinTildes.Append(char.IsLetterOrDigit(letra) ? letra : ' ');
        }
        return sinTildes.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries);
    }

    private static RecomendacionDto? Resolver(RecomendacionDef? definicion, IReadOnlyDictionary<string, ServicioDto> servicios)
    {
        if (definicion is null)
            return null;

        var encontrados = definicion.Servicios
            .Select(nombre => servicios.GetValueOrDefault(nombre))
            .OfType<ServicioDto>()
            .ToList();
        return encontrados.Count > 0 ? new RecomendacionDto(definicion.Motivo, encontrados) : null;
    }

    private async Task<IReadOnlyDictionary<string, ServicioDto>> ServiciosPorNombreAsync(CancellationToken ct) =>
        (await catalogo.ListarServiciosAsync(ct)).ToDictionary(s => s.Nombre, StringComparer.OrdinalIgnoreCase);
}

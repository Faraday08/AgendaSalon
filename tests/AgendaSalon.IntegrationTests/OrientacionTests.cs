using AgendaSalon.Application;
using AgendaSalon.Application.Catalogo;
using AgendaSalon.Application.Orientacion;
using AgendaSalon.Domain.Comun;
using Microsoft.Extensions.DependencyInjection;

namespace AgendaSalon.IntegrationTests;

/// <summary>Reglas de orientación con un catálogo en memoria igual al menú de demostración del salón.</summary>
public sealed class OrientacionTests
{
    private static readonly string[] MenuCompleto =
    [
        "Corte de dama", "Corte de caballero", "Recorte de barba", "Corte y barba", "Tinte completo", "Balayage",
        "Lavado y blower", "Peinado para evento", "Manicure", "Pedicure", "Tratamiento de keratina"
    ];

    private static IOrientacionService Crear(params string[] servicios)
    {
        var catalogo = new CatalogoFalso(servicios.Length == 0 ? MenuCompleto : servicios);
        return new ServiceCollection()
            .AddApplication()
            .AddSingleton<ICatalogoService>(catalogo)
            .BuildServiceProvider()
            .GetRequiredService<IOrientacionService>();
    }

    private static async Task<string[]> Recomienda(string texto)
    {
        var resultado = await Crear().InterpretarAsync(texto);
        Assert.True(resultado.Entendido, $"No entendió: {texto}");
        Assert.NotNull(resultado.Recomendacion);
        return resultado.Recomendacion.Servicios.Select(s => s.Nombre).ToArray();
    }

    [Theory]
    [InlineData("¡Me CASO el sábado!", "Peinado para evento")]
    [InlineData("tengo el pelo muy encrespado", "Tratamiento de keratina")]
    [InlineData("quiero unas mechas rubias", "Balayage")]
    [InlineData("quiero pelarme y arreglarme la barba", "Corte y barba")]
    [InlineData("un corte para mi hija", "Corte de dama")]
    [InlineData("solo un lavado y blower", "Lavado y blower")]
    [InlineData("me quiero hacer el pedicure", "Pedicure")]
    public async Task TextoLibre_RecomiendaElServicioIndicado(string texto, string servicio)
    {
        Assert.Contains(servicio, await Recomienda(texto));
    }

    [Fact]
    public async Task TextoLibre_ManosYPies_RecomiendaAmbos()
    {
        Assert.Equal(["Manicure", "Pedicure"], await Recomienda("manicure y pedicure"));
    }

    [Theory]
    [InlineData("quiero tapar las canas", "color")]
    [InlineData("me quiero cortar el pelo", "corte")]
    [InlineData("necesito arreglarme las uñas", "unas")]
    [InlineData("corte para un niño", "corte-barba")]
    public async Task TextoLibre_SiFaltaUnDato_HaceLaSiguientePregunta(string texto, string pregunta)
    {
        var resultado = await Crear().InterpretarAsync(texto);

        Assert.True(resultado.Entendido);
        Assert.Equal(pregunta, resultado.Pregunta);
    }

    [Theory]
    [InlineData("hola")]
    [InlineData("¿a qué hora abren?")]
    public async Task TextoSinPalabrasConocidas_VuelveALaPrimeraPregunta(string texto)
    {
        var resultado = await Crear().InterpretarAsync(texto);

        Assert.False(resultado.Entendido);
        Assert.Equal("inicio", resultado.Pregunta);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task TextoVacio_SeRechaza(string texto)
    {
        await Assert.ThrowsAsync<ReglaNegocioException>(() => Crear().InterpretarAsync(texto));
    }

    [Fact]
    public async Task Guia_CadaOpcionLlevaAUnaPreguntaExistenteOAUnaRecomendacion()
    {
        var guia = await Crear().ObtenerGuiaAsync();
        var ids = guia.Preguntas.Select(p => p.Id).ToHashSet();

        Assert.Contains(guia.PreguntaInicial, ids);
        Assert.All(guia.Preguntas.SelectMany(p => p.Opciones), o =>
            Assert.True(o.SiguientePregunta is not null ? ids.Contains(o.SiguientePregunta) : o.Recomendacion?.Servicios.Count > 0, o.Texto));
    }

    [Fact]
    public async Task Guia_SiUnServicioYaNoSeOfrece_OcultaLaOpcionQueLlevaAEl()
    {
        var sinBalayage = MenuCompleto.Where(s => s != "Balayage").ToArray();

        var guia = await Crear(sinBalayage).ObtenerGuiaAsync();

        var color = guia.Preguntas.Single(p => p.Id == "color");
        Assert.DoesNotContain(color.Opciones, o => o.Texto.Contains("Mechas", StringComparison.Ordinal));
        var noSeguro = color.Opciones.Single(o => o.Texto == "No estoy seguro");
        Assert.Equal(["Tinte completo"], noSeguro.Recomendacion!.Servicios.Select(s => s.Nombre));
    }

    private sealed class CatalogoFalso(IEnumerable<string> nombres) : ICatalogoService
    {
        private readonly List<ServicioDto> _servicios = nombres
            .Select((nombre, i) => new ServicioDto(i + 1, nombre, nombre, "General", 45, 500m))
            .ToList();

        public Task<IReadOnlyList<ServicioDto>> ListarServiciosAsync(CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<ServicioDto>>(_servicios);

        public Task<IReadOnlyList<EstilistaDto>> ListarEstilistasAsync(int? servicioId, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<EstilistaDto>>([]);
    }
}

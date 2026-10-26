using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Channels;
using AgendaSalon.Application.Abstracciones;
using AgendaSalon.Application.Autenticacion;
using AgendaSalon.Application.Catalogo;
using AgendaSalon.Application.Panel;
using AgendaSalon.Application.Reservas;
using AgendaSalon.Application.Usuarios;
using AgendaSalon.Domain.Citas;
using AgendaSalon.Infrastructure.Persistencia;
using AgendaSalon.IntegrationTests.Soporte;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AgendaSalon.IntegrationTests;

/// <summary>
/// Levanta la API completa en memoria (con SQLite en lugar de SQL Server) y la usa como lo haría el panel:
/// login por HTTP, endpoints protegidos con JWT y avisos en tiempo real por SignalR.
/// Usa los datos de demostración: Carla (colorista) y Luis (barbero) trabajan el martes 3 de noviembre de 2026.
/// </summary>
public sealed class ApiPanelTests : IAsyncLifetime
{
    private const string CorreoAdmin = "admin@salon.test";
    private const string ContrasenaAdmin = "AdminPrueba2026";
    private const string CorreoLuis = "luis@salon.test";
    private const string ContrasenaLuis = "LuisPrueba2026";
    private static readonly DateTime Ahora = new(2026, 11, 2, 8, 0, 0);
    private static readonly DateOnly Martes = new(2026, 11, 3);
    private static readonly TimeSpan EsperaMaxima = TimeSpan.FromSeconds(10);
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

    private readonly SqliteConnection _conexion = new("Data Source=:memory:");
    private WebApplicationFactory<Program> _fabrica = null!;
    private HttpClient _http = null!;
    private readonly List<HubConnection> _hubs = [];

    public async Task InitializeAsync()
    {
        await _conexion.OpenAsync();
        _fabrica = new WebApplicationFactory<Program>().WithWebHostBuilder(web =>
        {
            web.UseEnvironment("Testing");
            web.UseSetting("Jwt:Clave", new string('x', 48));
            web.UseSetting("AdminInicial:Correo", CorreoAdmin);
            web.UseSetting("AdminInicial:Contrasena", ContrasenaAdmin);
            web.ConfigureServices(services =>
            {
                services.RemoveAll<IDbContextOptionsConfiguration<AgendaDbContext>>();
                services.AddDbContext<AgendaDbContext>(o => o.UseSqlite(_conexion));
                services.RemoveAll<IRelojSalon>();
                services.AddSingleton<IRelojSalon>(new RelojFijo(Ahora));
            });
        });
        _http = _fabrica.CreateClient();
    }

    public async Task DisposeAsync()
    {
        foreach (var hub in _hubs)
            await hub.DisposeAsync();
        _http.Dispose();
        await _fabrica.DisposeAsync();
        await _conexion.DisposeAsync();
    }

    [Fact]
    public async Task Login_ConContrasenaIncorrecta_Devuelve401()
    {
        var respuesta = await _http.PostAsJsonAsync("api/auth/login", new IniciarSesionRequest(CorreoAdmin, "Incorrecta2026"));

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }

    [Fact]
    public async Task Panel_SinToken_Devuelve401_YConToken_Devuelve200()
    {
        var sinToken = await _http.GetAsync(new Uri("api/panel/citas?desde=2026-11-03&hasta=2026-11-03", UriKind.Relative));
        var token = await IniciarSesionAsync(CorreoAdmin, ContrasenaAdmin);
        var conToken = await EnviarAsync(HttpMethod.Get, "api/panel/citas?desde=2026-11-03&hasta=2026-11-03", token);

        Assert.Equal(HttpStatusCode.Unauthorized, sinToken.StatusCode);
        Assert.Equal(HttpStatusCode.OK, conToken.StatusCode);
    }

    [Fact]
    public async Task Login_DespuesDeCincoIntentos_Devuelve429()
    {
        var codigos = new List<HttpStatusCode>();
        for (var intento = 0; intento < 6; intento++)
        {
            var respuesta = await _http.PostAsJsonAsync("api/auth/login", new IniciarSesionRequest(CorreoAdmin, "Incorrecta2026"));
            codigos.Add(respuesta.StatusCode);
        }

        Assert.All(codigos.Take(5), c => Assert.Equal(HttpStatusCode.Unauthorized, c));
        Assert.Equal(HttpStatusCode.TooManyRequests, codigos[5]);
    }

    [Fact]
    public async Task Hub_SinToken_RechazaLaConexion()
    {
        var hub = CrearHub(token: null);

        var error = await Assert.ThrowsAsync<HttpRequestException>(() => hub.StartAsync());
        Assert.Equal(HttpStatusCode.Unauthorized, error.StatusCode);
    }

    [Fact]
    public async Task Hub_AdminRecibeTodasLasCitas_YCadaEstilistaSoloLasSuyas()
    {
        var (adminToken, luisToken, luisId, carlaId) = await PrepararAdminYLuisAsync();
        var avisosAdmin = await EscucharAsync(adminToken);
        var avisosLuis = await EscucharAsync(luisToken);

        var citaLuis = await ReservarAsync("Corte de caballero", luisId, 10);
        var citaCarla = await ReservarAsync("Corte de dama", carlaId, 10);

        // El administrador recibe las dos; Luis solo la suya.
        var recibidasAdmin = new[] { await avisosAdmin.SiguienteAsync(), await avisosAdmin.SiguienteAsync() };
        Assert.Equal(
            new[] { citaLuis.Codigo, citaCarla.Codigo }.Order(),
            recibidasAdmin.Select(a => a.Cita.Codigo).Order());
        var recibidaLuis = await avisosLuis.SiguienteAsync();
        Assert.Equal(("CitaCreada", citaLuis.Codigo), (recibidaLuis.Evento, recibidaLuis.Cita.Codigo));
        Assert.Equal("809-555-1234", recibidaLuis.Cita.ClienteTelefono);
        Assert.False(await avisosLuis.HayOtroAsync(TimeSpan.FromMilliseconds(500)));

        // Cuando el salón cancela la cita de Luis, a él le llega la actualización.
        var cancelacion = await EnviarAsync(HttpMethod.Post, $"api/panel/citas/{citaLuis.Codigo}/cancelacion", adminToken);
        Assert.Equal(HttpStatusCode.OK, cancelacion.StatusCode);
        var actualizada = await avisosLuis.SiguienteAsync();
        Assert.Equal(("CitaActualizada", EstadoCita.Cancelada), (actualizada.Evento, actualizada.Cita.Estado));
    }

    [Fact]
    public async Task Estilista_SoloVeSuAgenda_YNoPuedeGestionarNiCrearCuentas()
    {
        var (_, luisToken, luisId, carlaId) = await PrepararAdminYLuisAsync();
        await ReservarAsync("Corte de caballero", luisId, 10);
        var citaCarla = await ReservarAsync("Corte de dama", carlaId, 10);

        var agenda = await (await EnviarAsync(HttpMethod.Get, "api/panel/citas?desde=2026-11-03&hasta=2026-11-03", luisToken))
            .Content.ReadFromJsonAsync<List<CitaPanelDto>>(Json);
        var gestionarAjena = await EnviarAsync(HttpMethod.Post, $"api/panel/citas/{citaCarla.Codigo}/cancelacion", luisToken);
        var crearCuenta = await EnviarAsync(HttpMethod.Post, "api/panel/usuarios/estilistas", luisToken,
            new CrearCuentaEstilistaRequest("Carla", "carla@salon.test", "CarlaPrueba2026", carlaId));

        Assert.All(agenda!, c => Assert.Equal(luisId, c.EstilistaId));
        Assert.Single(agenda!);
        Assert.Equal(HttpStatusCode.Forbidden, gestionarAjena.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, crearCuenta.StatusCode);
    }

    private async Task<(string AdminToken, string LuisToken, int LuisId, int CarlaId)> PrepararAdminYLuisAsync()
    {
        var estilistas = await _http.GetFromJsonAsync<List<EstilistaDto>>("api/estilistas", Json);
        var luisId = estilistas!.Single(e => e.Nombre == "Luis Peña").Id;
        var carlaId = estilistas!.Single(e => e.Nombre == "Carla Méndez").Id;

        var adminToken = await IniciarSesionAsync(CorreoAdmin, ContrasenaAdmin);
        var creada = await EnviarAsync(HttpMethod.Post, "api/panel/usuarios/estilistas", adminToken,
            new CrearCuentaEstilistaRequest("Luis Peña", CorreoLuis, ContrasenaLuis, luisId));
        Assert.Equal(HttpStatusCode.Created, creada.StatusCode);

        return (adminToken, await IniciarSesionAsync(CorreoLuis, ContrasenaLuis), luisId, carlaId);
    }

    private async Task<string> IniciarSesionAsync(string correo, string contrasena)
    {
        var respuesta = await _http.PostAsJsonAsync("api/auth/login", new IniciarSesionRequest(correo, contrasena));
        respuesta.EnsureSuccessStatusCode();
        return (await respuesta.Content.ReadFromJsonAsync<SesionDto>(Json))!.Token;
    }

    private async Task<CitaDto> ReservarAsync(string servicio, int estilistaId, int hora)
    {
        var servicios = await _http.GetFromJsonAsync<List<ServicioDto>>("api/servicios", Json);
        var solicitud = new CrearCitaRequest(
            servicios!.Single(s => s.Nombre == servicio).Id, estilistaId, Martes.ToDateTime(new TimeOnly(hora, 0)),
            "María Rodríguez", "809-555-1234", null);

        var respuesta = await _http.PostAsJsonAsync("api/citas", solicitud, Json);
        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        return (await respuesta.Content.ReadFromJsonAsync<CitaDto>(Json))!;
    }

    private async Task<HttpResponseMessage> EnviarAsync(HttpMethod metodo, string ruta, string token, object? cuerpo = null)
    {
        using var peticion = new HttpRequestMessage(metodo, new Uri(ruta, UriKind.Relative));
        peticion.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (cuerpo is not null)
            peticion.Content = JsonContent.Create(cuerpo, options: Json);
        return await _http.SendAsync(peticion);
    }

    private HubConnection CrearHub(string? token)
    {
        var hub = new HubConnectionBuilder()
            .WithUrl(new Uri(_fabrica.Server.BaseAddress, "hubs/agenda"), o =>
            {
                // El servidor de pruebas no expone WebSockets reales; long polling usa el mismo canal HTTP.
                o.Transports = HttpTransportType.LongPolling;
                o.HttpMessageHandlerFactory = _ => _fabrica.Server.CreateHandler();
                if (token is not null)
                    o.AccessTokenProvider = () => Task.FromResult<string?>(token);
            })
            .AddJsonProtocol(o => o.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter()))
            .Build();
        _hubs.Add(hub);
        return hub;
    }

    private async Task<Escucha> EscucharAsync(string token)
    {
        var hub = CrearHub(token);
        var escucha = new Escucha();
        hub.On<CitaPanelDto>("CitaCreada", cita => escucha.Recibir("CitaCreada", cita));
        hub.On<CitaPanelDto>("CitaActualizada", cita => escucha.Recibir("CitaActualizada", cita));
        await hub.StartAsync();
        return escucha;
    }

    private sealed record Aviso(string Evento, CitaPanelDto Cita);

    private sealed class Escucha
    {
        private readonly Channel<Aviso> _avisos = Channel.CreateUnbounded<Aviso>();

        public void Recibir(string evento, CitaPanelDto cita) => _avisos.Writer.TryWrite(new Aviso(evento, cita));

        public async Task<Aviso> SiguienteAsync()
        {
            using var limite = new CancellationTokenSource(EsperaMaxima);
            return await _avisos.Reader.ReadAsync(limite.Token);
        }

        public async Task<bool> HayOtroAsync(TimeSpan espera)
        {
            using var limite = new CancellationTokenSource(espera);
            try
            {
                return await _avisos.Reader.WaitToReadAsync(limite.Token);
            }
            catch (OperationCanceledException)
            {
                return false;
            }
        }
    }
}

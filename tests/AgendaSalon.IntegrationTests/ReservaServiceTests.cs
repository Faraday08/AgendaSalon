using AgendaSalon.Application;
using AgendaSalon.Application.Abstracciones;
using AgendaSalon.Application.Comun;
using AgendaSalon.Application.Disponibilidad;
using AgendaSalon.Application.Reservas;
using AgendaSalon.Domain.Agenda;
using AgendaSalon.Domain.Citas;
using AgendaSalon.Domain.Comun;
using AgendaSalon.Domain.Estilistas;
using AgendaSalon.Domain.Servicios;
using AgendaSalon.Infrastructure;
using AgendaSalon.Infrastructure.Persistencia;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AgendaSalon.IntegrationTests;

/// <summary>
/// Prueban los casos de uso de punta a punta (Application + repositorios + EF Core) contra SQLite en memoria.
/// El "ahora" es el lunes 2 de noviembre de 2026 a las 8:00 y las citas se piden para el martes.
/// </summary>
public sealed class ReservaServiceTests : IAsyncLifetime
{
    private const string TelefonoMaria = "809-555-1234";
    private static readonly DateTime Ahora = new(2026, 11, 2, 8, 0, 0);
    private static readonly DateOnly Martes = new(2026, 11, 3);

    private readonly SqliteConnection _conexion = new("Data Source=:memory:");
    private ServiceProvider _proveedor = null!;
    private AsyncServiceScope _alcance;
    private AgendaDbContext _db = null!;
    private IReservaService _reservas = null!;
    private IDisponibilidadService _disponibilidad = null!;
    private int _corteId;
    private int _manicureId;
    private int _anaId;
    private int _betoId;

    public async Task InitializeAsync()
    {
        await _conexion.OpenAsync();

        var services = new ServiceCollection()
            .AddApplication()
            .AddPersistencia(o => o.UseSqlite(_conexion))
            .AddSingleton<IRelojSalon>(new RelojFijo(Ahora));
        services.Configure<ReglasAgenda>(r =>
        {
            r.IntervaloMinutos = 30;
            r.AnticipacionMinimaMinutos = 60;
            r.DiasMaximosAdelanto = 30;
        });

        _proveedor = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
        _alcance = _proveedor.CreateAsyncScope();
        _db = _alcance.ServiceProvider.GetRequiredService<AgendaDbContext>();
        await _db.Database.EnsureCreatedAsync();
        await SembrarAsync();

        _reservas = _alcance.ServiceProvider.GetRequiredService<IReservaService>();
        _disponibilidad = _alcance.ServiceProvider.GetRequiredService<IDisponibilidadService>();
    }

    public async Task DisposeAsync()
    {
        await _alcance.DisposeAsync();
        await _proveedor.DisposeAsync();
        await _conexion.DisposeAsync();
    }

    private async Task SembrarAsync()
    {
        var categoria = CategoriaServicio.Crear("General");
        var corte = Servicio.Crear("Corte", "Corte", categoria, 60, 500m);
        var manicure = Servicio.Crear("Manicure", "Manicure", categoria, 45, 600m);

        var ana = Estilista.Contratar("Ana", "Cortes");
        ana.AsignarServicio(corte);
        ana.AgregarJornada(DayOfWeek.Tuesday, new TimeOnly(9, 0), new TimeOnly(12, 0));

        var beto = Estilista.Contratar("Beto", "Cortes y uñas");
        beto.AsignarServicio(corte);
        beto.AsignarServicio(manicure);
        beto.AgregarJornada(DayOfWeek.Tuesday, new TimeOnly(9, 0), new TimeOnly(12, 0));

        _db.Estilistas.AddRange(ana, beto);
        await _db.SaveChangesAsync();
        (_corteId, _manicureId, _anaId, _betoId) = (corte.Id, manicure.Id, ana.Id, beto.Id);
    }

    private static DateTime ElMartesA(int hora, int minuto = 0) => Martes.ToDateTime(new TimeOnly(hora, minuto));

    private Task<CitaDto> Reservar(int? estilistaId, DateTime inicio, int? servicioId = null, string telefono = TelefonoMaria) =>
        _reservas.CrearAsync(new CrearCitaRequest(servicioId ?? _corteId, estilistaId, inicio, "María Rodríguez", telefono, null));

    [Fact]
    public async Task Crear_EnHoraLibre_QuedaConfirmadaConCodigoYCliente()
    {
        var cita = await Reservar(_anaId, ElMartesA(10));

        Assert.True(CodigoCita.TryCrear(cita.Codigo, out _));
        Assert.Equal(EstadoCita.Confirmada, cita.Estado);
        Assert.Equal("Ana", cita.Estilista);
        Assert.Equal(ElMartesA(11), cita.Fin);
        Assert.Equal(500m, cita.Precio);
        Assert.Equal("8095551234", (await _db.Clientes.SingleAsync()).Telefono.Valor);
    }

    [Fact]
    public async Task Crear_DosVecesConElMismoTelefono_ReutilizaElCliente()
    {
        await Reservar(_anaId, ElMartesA(9));
        await Reservar(_anaId, ElMartesA(11), telefono: "+1 (809) 555 1234");

        Assert.Equal(1, await _db.Clientes.CountAsync());
        Assert.Equal(2, await _db.Citas.CountAsync());
    }

    [Fact]
    public async Task Crear_ConHoraOcupadaDelMismoEstilista_LanzaConflicto()
    {
        await Reservar(_anaId, ElMartesA(10));

        await Assert.ThrowsAsync<ConflictoException>(() => Reservar(_anaId, ElMartesA(10, 30)));
    }

    [Fact]
    public async Task Crear_SinEstilista_AsignaAlQueEsteLibre()
    {
        await Reservar(_anaId, ElMartesA(10));

        var cita = await Reservar(null, ElMartesA(10));

        Assert.Equal("Beto", cita.Estilista);
    }

    [Fact]
    public async Task Crear_CuandoTodosEstanOcupados_LanzaConflicto()
    {
        await Reservar(_anaId, ElMartesA(10));
        await Reservar(_betoId, ElMartesA(10));

        await Assert.ThrowsAsync<ConflictoException>(() => Reservar(null, ElMartesA(10)));
    }

    [Fact]
    public async Task Crear_ConEstilistaQueNoHaceElServicio_LanzaReglaDeNegocio()
    {
        await Assert.ThrowsAsync<ReglaNegocioException>(() => Reservar(_anaId, ElMartesA(10), _manicureId));
    }

    [Fact]
    public async Task Crear_FueraDeLaRejillaDeHoras_LanzaConflicto()
    {
        await Assert.ThrowsAsync<ConflictoException>(() => Reservar(_anaId, ElMartesA(10, 10)));
    }

    [Fact]
    public async Task Crear_ConServicioInexistente_LanzaNoEncontrado()
    {
        await Assert.ThrowsAsync<NoEncontradoException>(() => Reservar(null, ElMartesA(10), servicioId: 999));
    }

    [Theory]
    [InlineData("555-1234")]
    [InlineData("")]
    public async Task Crear_ConTelefonoInvalido_NoGuardaNada(string telefono)
    {
        await Assert.ThrowsAsync<ReglaNegocioException>(() => Reservar(_anaId, ElMartesA(10), telefono: telefono));

        Assert.Equal(0, await _db.Citas.CountAsync());
    }

    [Fact]
    public async Task Disponibilidad_NoMuestraLaHoraYaReservada()
    {
        await Reservar(_anaId, ElMartesA(10));

        var huecos = await _disponibilidad.ConsultarAsync(_corteId, Martes, _anaId);

        Assert.Equal([ElMartesA(9), ElMartesA(11)], huecos.Select(h => h.Inicio));
    }

    [Fact]
    public async Task Cancelar_ConTelefonoDeOtraPersona_LanzaReglaDeNegocio()
    {
        var cita = await Reservar(_anaId, ElMartesA(10));

        await Assert.ThrowsAsync<ReglaNegocioException>(() => _reservas.CancelarAsync(cita.Codigo, "829-000-0000"));
    }

    [Fact]
    public async Task Cancelar_LiberaElHorarioYQuedaGuardado()
    {
        var cita = await Reservar(_anaId, ElMartesA(10));

        // El código no distingue mayúsculas y el teléfono acepta otro formato.
        await _reservas.CancelarAsync(cita.Codigo.ToLowerInvariant(), "+1 (809) 555 1234");
        var otra = await Reservar(_anaId, ElMartesA(10));

        Assert.Equal(EstadoCita.Confirmada, otra.Estado);
        Assert.Equal(EstadoCita.Cancelada, (await _reservas.ObtenerAsync(cita.Codigo)).Estado);
    }

    [Theory]
    [InlineData("ZZZZZZ")]
    [InlineData("no-es-un-codigo")]
    public async Task Obtener_ConCodigoInexistenteOMalFormado_LanzaNoEncontrado(string codigo)
    {
        await Assert.ThrowsAsync<NoEncontradoException>(() => _reservas.ObtenerAsync(codigo));
    }

    private sealed class RelojFijo(DateTime ahora) : IRelojSalon
    {
        public DateTime Ahora => ahora;
    }
}

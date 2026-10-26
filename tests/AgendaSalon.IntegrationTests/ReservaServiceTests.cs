using AgendaSalon.Application.Comun;
using AgendaSalon.Application.Disponibilidad;
using AgendaSalon.Application.Reservas;
using AgendaSalon.Domain.Citas;
using AgendaSalon.Domain.Comun;
using AgendaSalon.IntegrationTests.Soporte;
using Microsoft.EntityFrameworkCore;
using static AgendaSalon.IntegrationTests.Soporte.EntornoAgenda;

namespace AgendaSalon.IntegrationTests;

/// <summary>Casos de uso de reserva de punta a punta (Application + repositorios + EF Core) sobre SQLite en memoria.</summary>
public sealed class ReservaServiceTests : IAsyncLifetime
{
    private const string TelefonoMaria = "809-555-1234";

    private EntornoAgenda _entorno = null!;
    private IReservaService _reservas = null!;
    private IDisponibilidadService _disponibilidad = null!;

    public async Task InitializeAsync()
    {
        _entorno = await CrearAsync();
        _reservas = _entorno.Obtener<IReservaService>();
        _disponibilidad = _entorno.Obtener<IDisponibilidadService>();
    }

    public async Task DisposeAsync() => await _entorno.DisposeAsync();

    private Task<CitaDto> Reservar(int? estilistaId, DateTime inicio, int? servicioId = null, string telefono = TelefonoMaria) =>
        _reservas.CrearAsync(new CrearCitaRequest(servicioId ?? _entorno.CorteId, estilistaId, inicio, "María Rodríguez", telefono, null));

    [Fact]
    public async Task Crear_EnHoraLibre_QuedaConfirmadaConCodigoYCliente()
    {
        var cita = await Reservar(_entorno.AnaId, ElMartesA(10));

        Assert.True(CodigoCita.TryCrear(cita.Codigo, out _));
        Assert.Equal(EstadoCita.Confirmada, cita.Estado);
        Assert.Equal("Ana", cita.Estilista);
        Assert.Equal(ElMartesA(11), cita.Fin);
        Assert.Equal(500m, cita.Precio);
        Assert.Equal("8095551234", (await _entorno.Db.Clientes.SingleAsync()).Telefono.Valor);
    }

    [Fact]
    public async Task Crear_AvisaAlPanelConLosDatosDelCliente()
    {
        var cita = await Reservar(_entorno.AnaId, ElMartesA(10));

        var aviso = Assert.Single(_entorno.Notificador.Enviadas);
        Assert.Equal("CitaCreadaAsync", aviso.Evento);
        Assert.Equal(cita.Codigo, aviso.Cita.Codigo);
        Assert.Equal(_entorno.AnaId, aviso.Cita.EstilistaId);
        Assert.Equal("809-555-1234", aviso.Cita.ClienteTelefono);
    }

    [Fact]
    public async Task Crear_DosVecesConElMismoTelefono_ReutilizaElCliente()
    {
        await Reservar(_entorno.AnaId, ElMartesA(9));
        await Reservar(_entorno.AnaId, ElMartesA(11), telefono: "+1 (809) 555 1234");

        Assert.Equal(1, await _entorno.Db.Clientes.CountAsync());
        Assert.Equal(2, await _entorno.Db.Citas.CountAsync());
    }

    [Fact]
    public async Task Crear_ConHoraOcupadaDelMismoEstilista_LanzaConflicto()
    {
        await Reservar(_entorno.AnaId, ElMartesA(10));

        await Assert.ThrowsAsync<ConflictoException>(() => Reservar(_entorno.AnaId, ElMartesA(10, 30)));
    }

    [Fact]
    public async Task Crear_SinEstilista_AsignaAlQueEsteLibre()
    {
        await Reservar(_entorno.AnaId, ElMartesA(10));

        var cita = await Reservar(null, ElMartesA(10));

        Assert.Equal("Beto", cita.Estilista);
    }

    [Fact]
    public async Task Crear_CuandoTodosEstanOcupados_LanzaConflictoYNoAvisa()
    {
        await Reservar(_entorno.AnaId, ElMartesA(10));
        await Reservar(_entorno.BetoId, ElMartesA(10));

        await Assert.ThrowsAsync<ConflictoException>(() => Reservar(null, ElMartesA(10)));
        Assert.Equal(2, _entorno.Notificador.Enviadas.Count);
    }

    [Fact]
    public async Task Crear_ConEstilistaQueNoHaceElServicio_LanzaReglaDeNegocio()
    {
        await Assert.ThrowsAsync<ReglaNegocioException>(() => Reservar(_entorno.AnaId, ElMartesA(10), _entorno.ManicureId));
    }

    [Fact]
    public async Task Crear_FueraDeLaRejillaDeHoras_LanzaConflicto()
    {
        await Assert.ThrowsAsync<ConflictoException>(() => Reservar(_entorno.AnaId, ElMartesA(10, 10)));
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
        await Assert.ThrowsAsync<ReglaNegocioException>(() => Reservar(_entorno.AnaId, ElMartesA(10), telefono: telefono));

        Assert.Equal(0, await _entorno.Db.Citas.CountAsync());
    }

    [Fact]
    public async Task Disponibilidad_NoMuestraLaHoraYaReservada()
    {
        await Reservar(_entorno.AnaId, ElMartesA(10));

        var huecos = await _disponibilidad.ConsultarAsync(_entorno.CorteId, Martes, _entorno.AnaId);

        Assert.Equal([ElMartesA(9), ElMartesA(11)], huecos.Select(h => h.Inicio));
    }

    [Fact]
    public async Task Cancelar_ConTelefonoDeOtraPersona_LanzaReglaDeNegocio()
    {
        var cita = await Reservar(_entorno.AnaId, ElMartesA(10));

        await Assert.ThrowsAsync<ReglaNegocioException>(() => _reservas.CancelarAsync(cita.Codigo, "829-000-0000"));
    }

    [Fact]
    public async Task Cancelar_LiberaElHorarioYAvisaAlPanel()
    {
        var cita = await Reservar(_entorno.AnaId, ElMartesA(10));

        // El código no distingue mayúsculas y el teléfono acepta otro formato.
        await _reservas.CancelarAsync(cita.Codigo.ToLowerInvariant(), "+1 (809) 555 1234");
        var otra = await Reservar(_entorno.AnaId, ElMartesA(10));

        Assert.Equal(EstadoCita.Confirmada, otra.Estado);
        Assert.Equal(EstadoCita.Cancelada, (await _reservas.ObtenerAsync(cita.Codigo)).Estado);
        Assert.Contains(_entorno.Notificador.Enviadas,
            n => n.Evento == "CitaActualizadaAsync" && n.Cita.Codigo == cita.Codigo && n.Cita.Estado == EstadoCita.Cancelada);
    }

    [Theory]
    [InlineData("ZZZZZZ")]
    [InlineData("no-es-un-codigo")]
    public async Task Obtener_ConCodigoInexistenteOMalFormado_LanzaNoEncontrado(string codigo)
    {
        await Assert.ThrowsAsync<NoEncontradoException>(() => _reservas.ObtenerAsync(codigo));
    }
}

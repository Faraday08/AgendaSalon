using AgendaSalon.Application.Autenticacion;
using AgendaSalon.Application.Comun;
using AgendaSalon.Application.Panel;
using AgendaSalon.Application.Reservas;
using AgendaSalon.Application.Usuarios;
using AgendaSalon.Domain.Citas;
using AgendaSalon.Domain.Comun;
using AgendaSalon.Domain.Usuarios;
using AgendaSalon.Infrastructure.Seguridad;
using AgendaSalon.IntegrationTests.Soporte;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.JsonWebTokens;
using static AgendaSalon.IntegrationTests.Soporte.EntornoAgenda;

namespace AgendaSalon.IntegrationTests;

public sealed class PanelYAutenticacionTests : IAsyncLifetime
{
    private const string ContrasenaAna = "AnaPrueba2026";

    private EntornoAgenda _entorno = null!;
    private IAutenticacionService _autenticacion = null!;
    private IUsuarioService _usuarios = null!;
    private IPanelCitasService _panel = null!;

    public async Task InitializeAsync()
    {
        _entorno = await CrearAsync();
        _autenticacion = _entorno.Obtener<IAutenticacionService>();
        _usuarios = _entorno.Obtener<IUsuarioService>();
        _panel = _entorno.Obtener<IPanelCitasService>();
    }

    public async Task DisposeAsync() => await _entorno.DisposeAsync();

    private Task<UsuarioDto> CrearCuentaAnaComoAdmin(string correo = "ana@salon.test", string contrasena = ContrasenaAna)
    {
        _entorno.Usuario.ComoAdministrador();
        return _usuarios.CrearCuentaEstilistaAsync(new CrearCuentaEstilistaRequest("Ana", correo, contrasena, _entorno.AnaId));
    }

    private Task<CitaDto> Reservar(int estilistaId, int hora) =>
        _entorno.Obtener<IReservaService>().CrearAsync(
            new CrearCitaRequest(_entorno.CorteId, estilistaId, ElMartesA(hora), "María Rodríguez", "809-555-1234", null));

    [Fact]
    public async Task IniciarSesion_ConCredencialesCorrectas_DevuelveTokenConRol()
    {
        var sesion = await _autenticacion.IniciarSesionAsync(new IniciarSesionRequest(" ADMIN@salon.test ", ContrasenaAdmin));

        var token = new JsonWebTokenHandler().ReadJsonWebToken(sesion.Token);
        Assert.Equal(RolUsuario.Administrador, sesion.Rol);
        Assert.Equal("Administrador", token.GetClaim(ClaimsAgenda.Rol).Value);
        Assert.True(sesion.ExpiraEnUtc > DateTime.UtcNow);
    }

    [Fact]
    public async Task IniciarSesion_CuentaDeEstilista_IncluyeSuEstilistaEnElToken()
    {
        await CrearCuentaAnaComoAdmin();

        var sesion = await _autenticacion.IniciarSesionAsync(new IniciarSesionRequest("ana@salon.test", ContrasenaAna));

        var token = new JsonWebTokenHandler().ReadJsonWebToken(sesion.Token);
        Assert.Equal(_entorno.AnaId.ToString(System.Globalization.CultureInfo.InvariantCulture), token.GetClaim(ClaimsAgenda.EstilistaId).Value);
    }

    [Theory]
    [InlineData(CorreoAdmin, "ContraseñaIncorrecta1")]
    [InlineData("nadie@salon.test", ContrasenaAdmin)]
    [InlineData("no-es-un-correo", ContrasenaAdmin)]
    public async Task IniciarSesion_ConCredencialesIncorrectas_DaElMismoErrorSiempre(string correo, string contrasena)
    {
        var error = await Assert.ThrowsAsync<NoAutorizadoException>(() =>
            _autenticacion.IniciarSesionAsync(new IniciarSesionRequest(correo, contrasena)));

        Assert.Equal("Correo o contraseña incorrectos.", error.Message);
    }

    [Fact]
    public async Task IniciarSesion_ConCuentaDesactivada_SeRechaza()
    {
        var admin = await _entorno.Db.Usuarios.SingleAsync();
        admin.Desactivar();
        await _entorno.Db.SaveChangesAsync();

        await Assert.ThrowsAsync<NoAutorizadoException>(() =>
            _autenticacion.IniciarSesionAsync(new IniciarSesionRequest(CorreoAdmin, ContrasenaAdmin)));
    }

    [Fact]
    public async Task CrearCuenta_GuardaElHashYNoLaContrasena()
    {
        await CrearCuentaAnaComoAdmin();

        var cuenta = await _entorno.Db.Usuarios.SingleAsync(u => u.Rol == RolUsuario.Estilista);
        Assert.NotEqual(ContrasenaAna, cuenta.HashContrasena);
        Assert.DoesNotContain(ContrasenaAna, cuenta.HashContrasena, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CrearCuenta_SiendoEstilista_EstaProhibido()
    {
        _entorno.Usuario.ComoEstilista(_entorno.BetoId);

        await Assert.ThrowsAsync<ProhibidoException>(() =>
            _usuarios.CrearCuentaEstilistaAsync(new CrearCuentaEstilistaRequest("Ana", "ana@salon.test", ContrasenaAna, _entorno.AnaId)));
    }

    [Fact]
    public async Task CrearCuenta_ConCorreoRepetidoOEstilistaConCuenta_LanzaConflicto()
    {
        await CrearCuentaAnaComoAdmin();

        await Assert.ThrowsAsync<ConflictoException>(() => CrearCuentaAnaComoAdmin(correo: "otra@salon.test"));
        await Assert.ThrowsAsync<ConflictoException>(() =>
            _usuarios.CrearCuentaEstilistaAsync(new CrearCuentaEstilistaRequest("Beto", "ana@salon.test", ContrasenaAna, _entorno.BetoId)));
    }

    [Fact]
    public async Task CrearCuenta_ConContrasenaDebil_LanzaReglaDeNegocio()
    {
        await Assert.ThrowsAsync<ReglaNegocioException>(() => CrearCuentaAnaComoAdmin(contrasena: "123"));
    }

    [Fact]
    public async Task Panel_EstilistaSoloVeSusCitas_AunquePidaLasDeOtro()
    {
        await Reservar(_entorno.AnaId, 9);
        await Reservar(_entorno.BetoId, 10);
        _entorno.Usuario.ComoEstilista(_entorno.AnaId);

        var citas = await _panel.ListarAsync(Martes, Martes, estilistaId: _entorno.BetoId);

        var cita = Assert.Single(citas);
        Assert.Equal(_entorno.AnaId, cita.EstilistaId);
    }

    [Fact]
    public async Task Panel_AdministradorVeTodasOrdenadasPorHora()
    {
        await Reservar(_entorno.BetoId, 10);
        await Reservar(_entorno.AnaId, 9);
        _entorno.Usuario.ComoAdministrador();

        var citas = await _panel.ListarAsync(Martes, Martes, estilistaId: null);

        Assert.Equal([ElMartesA(9), ElMartesA(10)], citas.Select(c => c.Inicio));
    }

    [Fact]
    public async Task Panel_SinSesion_LanzaNoAutorizado()
    {
        await Assert.ThrowsAsync<NoAutorizadoException>(() => _panel.ListarAsync(Martes, Martes, null));
    }

    [Fact]
    public async Task Panel_RangoMayorA31Dias_LanzaReglaDeNegocio()
    {
        _entorno.Usuario.ComoAdministrador();

        await Assert.ThrowsAsync<ReglaNegocioException>(() => _panel.ListarAsync(Martes, Martes.AddDays(31), null));
    }

    [Fact]
    public async Task Panel_EstilistaNoPuedeGestionarCitasDeOtro()
    {
        var citaDeBeto = await Reservar(_entorno.BetoId, 10);
        _entorno.Usuario.ComoEstilista(_entorno.AnaId);

        await Assert.ThrowsAsync<ProhibidoException>(() => _panel.CancelarAsync(citaDeBeto.Codigo));
    }

    [Fact]
    public async Task Panel_AdministradorCancela_GuardaYAvisa()
    {
        var cita = await Reservar(_entorno.BetoId, 10);
        _entorno.Usuario.ComoAdministrador();

        var cancelada = await _panel.CancelarAsync(cita.Codigo);

        Assert.Equal(EstadoCita.Cancelada, cancelada.Estado);
        Assert.Equal(EstadoCita.Cancelada, (await _entorno.Db.Citas.AsNoTracking().SingleAsync()).Estado);
        Assert.Equal("CitaActualizadaAsync", _entorno.Notificador.Enviadas[^1].Evento);
    }
}

using AgendaSalon.Domain.Citas;
using AgendaSalon.Domain.Comun;
using AgendaSalon.Domain.Usuarios;
using static AgendaSalon.Domain.Tests.Datos;

namespace AgendaSalon.Domain.Tests;

public class UsuarioTests
{
    private const string HashCualquiera = "hash-de-prueba";

    [Theory]
    [InlineData("  Carla@Salon.COM ", "carla@salon.com")]
    [InlineData("luis.pena@correo.com.do", "luis.pena@correo.com.do")]
    public void Correo_SeNormalizaEnMinusculasYSinEspacios(string texto, string esperado)
    {
        Assert.Equal(esperado, Correo.Crear(texto).Valor);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("sin-arroba.com")]
    [InlineData("nombre@sin-punto")]
    [InlineData("Nombre <nombre@salon.com>")]
    public void Correo_Invalido_SeRechaza(string? texto)
    {
        Assert.Throws<ReglaNegocioException>(() => Correo.Crear(texto));
    }

    [Theory]
    [InlineData("corta1")]
    [InlineData("soloLetrasSinNumeros")]
    [InlineData("1234567890123")]
    public void PoliticaContrasenas_RechazaContrasenasDebiles(string contrasena)
    {
        Assert.Throws<ReglaNegocioException>(() => PoliticaContrasenas.Validar(contrasena));
    }

    [Fact]
    public void PoliticaContrasenas_AceptaLetrasYNumerosConLongitudSuficiente()
    {
        PoliticaContrasenas.Validar("Salon2026Seguro");
    }

    [Fact]
    public void CrearParaEstilista_AsociaElEstilistaYElRol()
    {
        var estilista = EstilistaDeMartes();

        var usuario = Usuario.CrearParaEstilista("Ana", Correo.Crear("ana@salon.com"), HashCualquiera, estilista, Ahora);

        Assert.Equal(RolUsuario.Estilista, usuario.Rol);
        Assert.Same(estilista, usuario.Estilista);
        Assert.True(usuario.Activo);
    }

    [Fact]
    public void CrearParaEstilista_ConEstilistaInactivo_SeRechaza()
    {
        var estilista = EstilistaDeMartes();
        estilista.Desactivar();

        Assert.Throws<ReglaNegocioException>(() =>
            Usuario.CrearParaEstilista("Ana", Correo.Crear("ana@salon.com"), HashCualquiera, estilista, Ahora));
    }

    [Fact]
    public void CrearAdministrador_NoTieneEstilista()
    {
        var usuario = Usuario.CrearAdministrador("Admin", Correo.Crear("admin@salon.com"), HashCualquiera, Ahora);

        Assert.Equal(RolUsuario.Administrador, usuario.Rol);
        Assert.Null(usuario.EstilistaId);
    }
}

public class CancelacionPorSalonTests
{
    private static Cita ReservarCorteALas10()
    {
        var corte = Corte();
        return Cita.Reservar(CodigoCita.Generar(), Maria(), corte, EstilistaDeMartes(corte), ElMartesA(10), null, Ahora);
    }

    [Fact]
    public void CancelarPorSalon_DuranteLaCita_SePermite()
    {
        var cita = ReservarCorteALas10();

        cita.CancelarPorSalon(ElMartesA(10, 30));

        Assert.Equal(EstadoCita.Cancelada, cita.Estado);
    }

    [Fact]
    public void CancelarPorSalon_DespuesDeTerminar_SeRechaza()
    {
        var cita = ReservarCorteALas10();

        Assert.Throws<ReglaNegocioException>(() => cita.CancelarPorSalon(ElMartesA(11)));
    }

    [Fact]
    public void CancelarPorSalon_UnaCitaCompletada_SeRechaza()
    {
        var cita = ReservarCorteALas10();
        cita.MarcarCompletada(ElMartesA(11));

        Assert.Throws<ReglaNegocioException>(() => cita.CancelarPorSalon(ElMartesA(10, 30)));
    }
}

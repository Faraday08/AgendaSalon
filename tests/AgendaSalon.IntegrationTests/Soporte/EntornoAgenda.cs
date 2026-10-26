using AgendaSalon.Application;
using AgendaSalon.Application.Abstracciones;
using AgendaSalon.Domain.Agenda;
using AgendaSalon.Domain.Estilistas;
using AgendaSalon.Domain.Servicios;
using AgendaSalon.Domain.Usuarios;
using AgendaSalon.Infrastructure;
using AgendaSalon.Infrastructure.Persistencia;
using AgendaSalon.Infrastructure.Seguridad;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AgendaSalon.IntegrationTests.Soporte;

/// <summary>
/// Application + Infrastructure reales sobre SQLite en memoria, con dobles para el reloj, el usuario actual
/// y las notificaciones. El "ahora" es el lunes 2 de noviembre de 2026 a las 8:00.
/// Datos: servicios Corte (60 min) y Manicure (45 min); Ana hace cortes y Beto hace ambos, los martes de 9:00 a 12:00.
/// </summary>
internal sealed class EntornoAgenda : IAsyncDisposable
{
    public const string CorreoAdmin = "admin@salon.test";
    public const string ContrasenaAdmin = "AdminPrueba2026";

    public static readonly DateTime Ahora = new(2026, 11, 2, 8, 0, 0);
    public static readonly DateOnly Martes = new(2026, 11, 3);

    private readonly SqliteConnection _conexion;
    private readonly ServiceProvider _proveedor;
    private readonly AsyncServiceScope _alcance;

    private EntornoAgenda(SqliteConnection conexion, ServiceProvider proveedor, UsuarioActualFalso usuario, NotificadorFalso notificador)
    {
        _conexion = conexion;
        _proveedor = proveedor;
        _alcance = proveedor.CreateAsyncScope();
        Usuario = usuario;
        Notificador = notificador;
    }

    public UsuarioActualFalso Usuario { get; }
    public NotificadorFalso Notificador { get; }
    public AgendaDbContext Db => Obtener<AgendaDbContext>();
    public int CorteId { get; private set; }
    public int ManicureId { get; private set; }
    public int AnaId { get; private set; }
    public int BetoId { get; private set; }

    public static DateTime ElMartesA(int hora, int minuto = 0) => Martes.ToDateTime(new TimeOnly(hora, minuto));

    public T Obtener<T>() where T : notnull => _alcance.ServiceProvider.GetRequiredService<T>();

    /// <param name="configurar">Registros extra o reemplazos, aplicados al final.</param>
    public static async Task<EntornoAgenda> CrearAsync(Action<IServiceCollection>? configurar = null)
    {
        var conexion = new SqliteConnection("Data Source=:memory:");
        await conexion.OpenAsync();

        var usuario = new UsuarioActualFalso();
        var notificador = new NotificadorFalso();
        var services = new ServiceCollection()
            .AddLogging()
            .AddApplication()
            .AddSeguridad()
            .AddPersistencia(o => o.UseSqlite(conexion))
            .AddSingleton<IRelojSalon>(new RelojFijo(Ahora))
            .AddSingleton<IUsuarioActual>(usuario)
            .AddSingleton<INotificadorAgenda>(notificador);
        services.Configure<ReglasAgenda>(r =>
        {
            r.IntervaloMinutos = 30;
            r.AnticipacionMinimaMinutos = 60;
            r.DiasMaximosAdelanto = 30;
        });
        services.Configure<OpcionesJwt>(o =>
        {
            o.Emisor = "pruebas";
            o.Audiencia = "pruebas";
            o.Clave = new string('k', OpcionesJwt.BytesMinimosClave);
        });
        configurar?.Invoke(services);

        var proveedor = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
        var entorno = new EntornoAgenda(conexion, proveedor, usuario, notificador);
        await entorno.SembrarAsync();
        return entorno;
    }

    private async Task SembrarAsync()
    {
        await Db.Database.EnsureCreatedAsync();

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

        var hasher = Obtener<IHasherContrasenas>();
        var admin = Domain.Usuarios.Usuario.CrearAdministrador("Admin", Correo.Crear(CorreoAdmin), hasher.Hashear(ContrasenaAdmin), Ahora);

        Db.Estilistas.AddRange(ana, beto);
        Db.Usuarios.Add(admin);
        await Db.SaveChangesAsync();
        (CorteId, ManicureId, AnaId, BetoId) = (corte.Id, manicure.Id, ana.Id, beto.Id);
    }

    public async ValueTask DisposeAsync()
    {
        await _alcance.DisposeAsync();
        await _proveedor.DisposeAsync();
        await _conexion.DisposeAsync();
    }
}

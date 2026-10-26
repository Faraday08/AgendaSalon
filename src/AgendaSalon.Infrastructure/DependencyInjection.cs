using System.Text;
using AgendaSalon.Application.Abstracciones;
using AgendaSalon.Domain.Agenda;
using AgendaSalon.Domain.Citas;
using AgendaSalon.Domain.Clientes;
using AgendaSalon.Domain.Estilistas;
using AgendaSalon.Domain.Servicios;
using AgendaSalon.Domain.Usuarios;
using AgendaSalon.Infrastructure.Persistencia;
using AgendaSalon.Infrastructure.Persistencia.Repositorios;
using AgendaSalon.Infrastructure.Reloj;
using AgendaSalon.Infrastructure.Seguridad;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AgendaSalon.Infrastructure;

public static class DependencyInjection
{
    private const string SeccionAgenda = "Agenda";
    private const string SeccionJwt = "Jwt";
    private const string SeccionAdminInicial = "AdminInicial";

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        var cadenaConexion = configuration.GetConnectionString("Agenda")
            ?? throw new InvalidOperationException("Falta la cadena de conexión 'Agenda' en appsettings.json.");

        services.Configure<ReglasAgenda>(configuration.GetSection(SeccionAgenda));
        services.Configure<OpcionesReloj>(configuration.GetSection(SeccionAgenda));
        services.Configure<OpcionesAdminInicial>(configuration.GetSection(SeccionAdminInicial));
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IRelojSalon, RelojSalon>();

        services.AddOptions<OpcionesJwt>()
            .Bind(configuration.GetSection(SeccionJwt))
            .ValidateDataAnnotations()
            .Validate(
                o => Encoding.UTF8.GetByteCount(o.Clave) >= OpcionesJwt.BytesMinimosClave,
                $"Jwt:Clave debe tener al menos {OpcionesJwt.BytesMinimosClave} bytes. Configúrala con dotnet user-secrets.")
            .ValidateOnStart();

        return services
            .AddSeguridad()
            .AddPersistencia(o => o.UseSqlServer(cadenaConexion));
    }

    /// <summary>Hash de contraseñas y emisión de tokens. Las opciones de JWT se configuran aparte.</summary>
    public static IServiceCollection AddSeguridad(this IServiceCollection services)
    {
        services.TryAddTimeProvider();
        services.AddSingleton<IHasherContrasenas, HasherContrasenas>();
        services.AddSingleton<IGeneradorToken, GeneradorTokenJwt>();
        return services;
    }

    /// <summary>Registra el DbContext, la unidad de trabajo y los repositorios. Las pruebas lo usan con SQLite.</summary>
    public static IServiceCollection AddPersistencia(
        this IServiceCollection services, Action<DbContextOptionsBuilder> configurarBaseDeDatos)
    {
        services.AddDbContext<AgendaDbContext>(configurarBaseDeDatos);
        services.AddScoped<IUnidadDeTrabajo, UnidadDeTrabajo>();
        services.AddScoped<IClienteRepositorio, ClienteRepositorio>();
        services.AddScoped<IServicioRepositorio, ServicioRepositorio>();
        services.AddScoped<IEstilistaRepositorio, EstilistaRepositorio>();
        services.AddScoped<ICitaRepositorio, CitaRepositorio>();
        services.AddScoped<IUsuarioRepositorio, UsuarioRepositorio>();
        return services;
    }

    /// <summary>
    /// Deja la base lista: en SQL Server aplica las migraciones; en otros proveedores (las pruebas usan SQLite)
    /// crea el esquema directamente. Después carga los datos de demostración y el administrador inicial.
    /// </summary>
    public static async Task PrepararBaseDeDatosAsync(this IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var proveedor = scope.ServiceProvider;
        var db = proveedor.GetRequiredService<AgendaDbContext>();

        if (db.Database.IsSqlServer())
            await db.Database.MigrateAsync();
        else
            await db.Database.EnsureCreatedAsync();

        await DatosIniciales.SembrarAsync(db);
        await DatosIniciales.CrearAdministradorInicialAsync(
            db,
            proveedor.GetRequiredService<IHasherContrasenas>(),
            proveedor.GetRequiredService<IOptions<OpcionesAdminInicial>>().Value,
            proveedor.GetRequiredService<IRelojSalon>().Ahora);
    }

    private static void TryAddTimeProvider(this IServiceCollection services)
    {
        if (services.All(s => s.ServiceType != typeof(TimeProvider)))
            services.AddSingleton(TimeProvider.System);
    }
}

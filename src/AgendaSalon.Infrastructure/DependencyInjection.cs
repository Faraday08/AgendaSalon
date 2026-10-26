using AgendaSalon.Application.Abstracciones;
using AgendaSalon.Domain.Agenda;
using AgendaSalon.Domain.Citas;
using AgendaSalon.Domain.Clientes;
using AgendaSalon.Domain.Estilistas;
using AgendaSalon.Domain.Servicios;
using AgendaSalon.Infrastructure.Persistencia;
using AgendaSalon.Infrastructure.Persistencia.Repositorios;
using AgendaSalon.Infrastructure.Reloj;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AgendaSalon.Infrastructure;

public static class DependencyInjection
{
    private const string SeccionAgenda = "Agenda";

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var cadenaConexion = configuration.GetConnectionString("Agenda")
            ?? throw new InvalidOperationException("Falta la cadena de conexión 'Agenda' en appsettings.json.");

        services.Configure<ReglasAgenda>(configuration.GetSection(SeccionAgenda));
        services.Configure<OpcionesReloj>(configuration.GetSection(SeccionAgenda));
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IRelojSalon, RelojSalon>();

        return services.AddPersistencia(o => o.UseSqlServer(cadenaConexion));
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
        return services;
    }

    /// <summary>Aplica las migraciones pendientes y carga los datos de demostración.</summary>
    public static async Task PrepararBaseDeDatosAsync(this IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AgendaDbContext>();
        await db.Database.MigrateAsync();
        await DatosIniciales.SembrarAsync(db);
    }
}

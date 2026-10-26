using AgendaSalon.Application.Catalogo;
using AgendaSalon.Application.Disponibilidad;
using AgendaSalon.Application.Reservas;
using Microsoft.Extensions.DependencyInjection;

namespace AgendaSalon.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<PlanificadorAgenda>();
        services.AddScoped<ICatalogoService, CatalogoService>();
        services.AddScoped<IDisponibilidadService, DisponibilidadService>();
        services.AddScoped<IReservaService, ReservaService>();
        return services;
    }
}

using AgendaSalon.Application.Autenticacion;
using AgendaSalon.Application.Catalogo;
using AgendaSalon.Application.Disponibilidad;
using AgendaSalon.Application.Orientacion;
using AgendaSalon.Application.Panel;
using AgendaSalon.Application.Reservas;
using AgendaSalon.Application.Usuarios;
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
        services.AddScoped<IAutenticacionService, AutenticacionService>();
        services.AddScoped<IUsuarioService, UsuarioService>();
        services.AddScoped<IPanelCitasService, PanelCitasService>();
        services.AddScoped<IOrientacionService, OrientacionService>();
        return services;
    }
}

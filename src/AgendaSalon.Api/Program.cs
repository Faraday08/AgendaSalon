using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using AgendaSalon.Api;
using AgendaSalon.Api.Seguridad;
using AgendaSalon.Api.TiempoReal;
using AgendaSalon.Application;
using AgendaSalon.Application.Abstracciones;
using AgendaSalon.Domain.Usuarios;
using AgendaSalon.Infrastructure;
using AgendaSalon.Infrastructure.Seguridad;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Composición de capas: la API es el único proyecto que conoce Application e Infrastructure a la vez.
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUsuarioActual, UsuarioActualHttp>();
builder.Services.AddSingleton<INotificadorAgenda, NotificadorAgendaSignalR>();

// Autenticación con JWT. Las opciones se leen de IOptions<OpcionesJwt> para que la validación al iniciar las cubra.
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IOptions<OpcionesJwt>>((opciones, jwt) =>
    {
        opciones.MapInboundClaims = false;
        opciones.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Value.Emisor,
            ValidateAudience = true,
            ValidAudience = jwt.Value.Audiencia,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = jwt.Value.CrearClaveFirma(),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = ClaimsAgenda.Nombre,
            RoleClaimType = ClaimsAgenda.Rol
        };
        opciones.Events = new JwtBearerEvents
        {
            // Los navegadores no pueden enviar encabezados en WebSockets: SignalR manda el token en la URL.
            OnMessageReceived = contexto =>
            {
                var token = contexto.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(token) && contexto.HttpContext.Request.Path.StartsWithSegments(AgendaHub.Ruta))
                    contexto.Token = token;
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorizationBuilder()
    .AddPolicy(PoliticasAutorizacion.Personal, p => p
        .RequireAuthenticatedUser()
        .RequireRole(nameof(RolUsuario.Administrador), nameof(RolUsuario.Estilista)))
    .AddPolicy(PoliticasAutorizacion.Administrador, p => p
        .RequireAuthenticatedUser()
        .RequireRole(nameof(RolUsuario.Administrador)));

builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.AddPolicy(PoliticasAutorizacion.LimiteLogin, contexto => RateLimitPartition.GetFixedWindowLimiter(
        contexto.Connection.RemoteIpAddress?.ToString() ?? "desconocida",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 5, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
});

builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddSignalR()
    .AddJsonProtocol(o => o.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ManejadorExcepciones>();
builder.Services.AddOpenApi();
builder.Services.AddCors(o => o.AddPolicy("Frontend", p => p
    .WithOrigins(builder.Configuration.GetSection("Cors:Origenes").Get<string[]>() ?? [])
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials()));

var app = builder.Build();

await app.Services.PrepararBaseDeDatosAsync();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors("Frontend");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<AgendaHub>(AgendaHub.Ruta);

await app.RunAsync();

/// <summary>Visible para las pruebas de integración con WebApplicationFactory.</summary>
public partial class Program;

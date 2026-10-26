using AgendaSalon.Domain.Citas;

namespace AgendaSalon.Application.Panel;

/// <summary>Vista de una cita para el personal del salón. A diferencia del DTO público, incluye el teléfono del cliente.</summary>
public sealed record CitaPanelDto(
    string Codigo,
    DateTime Inicio,
    DateTime Fin,
    EstadoCita Estado,
    int ServicioId,
    string Servicio,
    int EstilistaId,
    string Estilista,
    string ClienteNombre,
    string ClienteTelefono,
    decimal Precio,
    string? Notas)
{
    internal static CitaPanelDto Desde(Cita cita) => new(
        cita.Codigo.Valor,
        cita.Inicio,
        cita.Fin,
        cita.Estado,
        cita.Servicio.Id,
        cita.Servicio.Nombre,
        cita.Estilista.Id,
        cita.Estilista.Nombre,
        cita.Cliente.Nombre,
        cita.Cliente.Telefono.ToString(),
        cita.PrecioAcordado,
        cita.Notas);
}

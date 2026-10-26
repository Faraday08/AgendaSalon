using AgendaSalon.Domain.Citas;

namespace AgendaSalon.Application.Reservas;

/// <summary>Si <see cref="EstilistaId"/> es null, se asigna el primer estilista libre.</summary>
public sealed record CrearCitaRequest(
    int ServicioId,
    int? EstilistaId,
    DateTime Inicio,
    string ClienteNombre,
    string ClienteTelefono,
    string? Notas);

public sealed record CancelarCitaRequest(string Telefono);

public sealed record CitaDto(
    string Codigo,
    string Servicio,
    string Estilista,
    DateTime Inicio,
    DateTime Fin,
    EstadoCita Estado,
    string ClienteNombre,
    decimal Precio)
{
    internal static CitaDto Desde(Cita cita) => new(
        cita.Codigo.Valor,
        cita.Servicio.Nombre,
        cita.Estilista.Nombre,
        cita.Inicio,
        cita.Fin,
        cita.Estado,
        cita.Cliente.Nombre,
        cita.PrecioAcordado);
}

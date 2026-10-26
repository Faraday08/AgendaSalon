namespace AgendaSalon.Domain.Citas;

/// <summary>Los valores coinciden con los Id de la tabla catálogo EstadosCita.</summary>
public enum EstadoCita : byte
{
    Confirmada = 1,
    Cancelada = 2,
    Completada = 3,
    NoAsistio = 4
}

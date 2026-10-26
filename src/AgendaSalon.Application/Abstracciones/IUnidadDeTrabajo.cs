namespace AgendaSalon.Application.Abstracciones;

public interface IUnidadDeTrabajo
{
    Task GuardarCambiosAsync(CancellationToken ct = default);

    /// <summary>
    /// Ejecuta la operación en una transacción serializable: lo que se lee dentro no puede cambiar hasta confirmar.
    /// Si choca con otra transacción concurrente lanza <see cref="Comun.ConcurrenciaException"/>.
    /// </summary>
    Task<T> EjecutarEnTransaccionAsync<T>(Func<CancellationToken, Task<T>> operacion, CancellationToken ct = default);
}

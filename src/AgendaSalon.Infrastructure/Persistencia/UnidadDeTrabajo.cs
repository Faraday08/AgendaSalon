using System.Data;
using AgendaSalon.Application.Abstracciones;
using AgendaSalon.Application.Comun;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace AgendaSalon.Infrastructure.Persistencia;

internal sealed class UnidadDeTrabajo(AgendaDbContext db) : IUnidadDeTrabajo
{
    private const int ErrorInterbloqueoSqlServer = 1205;

    public Task GuardarCambiosAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);

    public async Task<T> EjecutarEnTransaccionAsync<T>(Func<CancellationToken, Task<T>> operacion, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(operacion);

        // Con reintentos activados (Azure SQL), EF exige que una transacción propia vaya dentro de su
        // estrategia de ejecución: si falla por un error transitorio, se repite la operación completa.
        var estrategia = db.Database.CreateExecutionStrategy();
        try
        {
            return await estrategia.ExecuteAsync(async () =>
            {
                // Cada intento empieza limpio, sin las entidades que dejó a medias el intento anterior.
                db.ChangeTracker.Clear();

                // Serializable bloquea el rango leído. Si dos transacciones chocan, SQL Server deshace una;
                // al reintentarla verá el horario ya ocupado y responderá con un conflicto normal.
                await using var transaccion = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
                var resultado = await operacion(ct);
                await transaccion.CommitAsync(ct);
                return resultado;
            });
        }
        catch (Exception ex) when (EsInterbloqueo(ex))
        {
            throw new ConcurrenciaException(ex);
        }
    }

    // EF Core envuelve el SqlException en DbUpdateException, InvalidOperationException o RetryLimitExceededException.
    private static bool EsInterbloqueo(Exception? ex)
    {
        for (; ex is not null; ex = ex.InnerException)
        {
            if (ex is SqlException { Number: ErrorInterbloqueoSqlServer })
                return true;
        }
        return false;
    }
}

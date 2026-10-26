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
        try
        {
            // Serializable bloquea el rango leído. Si dos transacciones chocan,
            // SQL Server elige a una como víctima de interbloqueo y la deshace.
            await using var transaccion = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
            var resultado = await operacion(ct);
            await transaccion.CommitAsync(ct);
            return resultado;
        }
        catch (Exception ex) when (EsInterbloqueo(ex))
        {
            throw new ConcurrenciaException(ex);
        }
    }

    // EF Core envuelve el SqlException en DbUpdateException y a veces en InvalidOperationException.
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

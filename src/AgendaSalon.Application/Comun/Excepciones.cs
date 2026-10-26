namespace AgendaSalon.Application.Comun;

/// <summary>El recurso pedido no existe. Se traduce a HTTP 404.</summary>
public sealed class NoEncontradoException(string mensaje) : Exception(mensaje);

/// <summary>Credenciales inválidas o sesión ausente. Se traduce a HTTP 401.</summary>
public sealed class NoAutorizadoException(string mensaje) : Exception(mensaje);

/// <summary>El usuario está identificado pero no tiene permiso para esta operación. Se traduce a HTTP 403.</summary>
public sealed class ProhibidoException(string mensaje) : Exception(mensaje);

/// <summary>La operación choca con el estado actual de los datos. Se traduce a HTTP 409.</summary>
public class ConflictoException(string mensaje) : Exception(mensaje);

/// <summary>Otra transacción modificó los mismos datos al mismo tiempo.</summary>
public sealed class ConcurrenciaException(Exception causa)
    : ConflictoException("Otra persona hizo un cambio al mismo tiempo. Vuelve a intentarlo.")
{
    public Exception Causa { get; } = causa;
}

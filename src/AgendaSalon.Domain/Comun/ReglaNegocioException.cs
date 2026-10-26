namespace AgendaSalon.Domain.Comun;

/// <summary>Una operación viola una regla del negocio. El mensaje se puede mostrar al cliente.</summary>
public sealed class ReglaNegocioException(string mensaje) : Exception(mensaje);

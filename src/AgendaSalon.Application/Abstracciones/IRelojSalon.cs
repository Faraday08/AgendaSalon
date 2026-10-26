namespace AgendaSalon.Application.Abstracciones;

/// <summary>Hora local del salón, para no depender de la zona horaria del servidor.</summary>
public interface IRelojSalon
{
    DateTime Ahora { get; }
}

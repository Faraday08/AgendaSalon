namespace AgendaSalon.Domain.Comun;

/// <summary>Base de las entidades con identidad propia.</summary>
public abstract class Entidad
{
    public int Id { get; protected set; }
}

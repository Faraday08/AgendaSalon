namespace AgendaSalon.Domain.Clientes;

public interface IClienteRepositorio
{
    Task<Cliente?> ObtenerPorTelefonoAsync(Telefono telefono, CancellationToken ct = default);

    void Agregar(Cliente cliente);
}

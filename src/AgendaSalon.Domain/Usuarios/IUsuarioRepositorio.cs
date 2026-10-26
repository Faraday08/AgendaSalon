namespace AgendaSalon.Domain.Usuarios;

public interface IUsuarioRepositorio
{
    Task<Usuario?> ObtenerPorCorreoAsync(Correo correo, CancellationToken ct = default);

    Task<bool> ExisteCorreoAsync(Correo correo, CancellationToken ct = default);

    Task<bool> ExisteCuentaParaEstilistaAsync(int estilistaId, CancellationToken ct = default);

    Task<bool> ExisteAdministradorAsync(CancellationToken ct = default);

    void Agregar(Usuario usuario);
}

using AgendaSalon.Domain.Estilistas;
using AgendaSalon.Domain.Servicios;

namespace AgendaSalon.Application.Catalogo;

public sealed record ServicioDto(int Id, string Nombre, string Descripcion, string Categoria, int DuracionMinutos, decimal Precio);

public sealed record EstilistaDto(int Id, string Nombre, string Especialidad);

public interface ICatalogoService
{
    Task<IReadOnlyList<ServicioDto>> ListarServiciosAsync(CancellationToken ct = default);

    /// <summary>Estilistas activos. Con <paramref name="servicioId"/>, solo los que realizan ese servicio.</summary>
    Task<IReadOnlyList<EstilistaDto>> ListarEstilistasAsync(int? servicioId, CancellationToken ct = default);
}

internal sealed class CatalogoService(IServicioRepositorio servicios, IEstilistaRepositorio estilistas) : ICatalogoService
{
    public async Task<IReadOnlyList<ServicioDto>> ListarServiciosAsync(CancellationToken ct = default) =>
        (await servicios.ListarActivosAsync(ct))
            .Select(s => new ServicioDto(s.Id, s.Nombre, s.Descripcion, s.Categoria.Nombre, s.DuracionMinutos, s.Precio))
            .ToList();

    public async Task<IReadOnlyList<EstilistaDto>> ListarEstilistasAsync(int? servicioId, CancellationToken ct = default) =>
        (await estilistas.ListarActivosAsync(servicioId, ct))
            .Select(e => new EstilistaDto(e.Id, e.Nombre, e.Especialidad))
            .ToList();
}

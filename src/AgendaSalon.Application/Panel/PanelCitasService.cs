using AgendaSalon.Application.Abstracciones;
using AgendaSalon.Application.Comun;
using AgendaSalon.Domain.Citas;
using AgendaSalon.Domain.Comun;
using AgendaSalon.Domain.Usuarios;

namespace AgendaSalon.Application.Panel;

public interface IPanelCitasService
{
    /// <summary>
    /// Citas entre dos fechas, ambas incluidas (máximo 31 días). Un administrador ve todas o filtra por estilista;
    /// un estilista solo ve las suyas.
    /// </summary>
    Task<IReadOnlyList<CitaPanelDto>> ListarAsync(DateOnly desde, DateOnly hasta, int? estilistaId, CancellationToken ct = default);

    Task<CitaPanelDto> MarcarCompletadaAsync(string codigo, CancellationToken ct = default);

    Task<CitaPanelDto> MarcarNoAsistioAsync(string codigo, CancellationToken ct = default);

    Task<CitaPanelDto> CancelarAsync(string codigo, CancellationToken ct = default);
}

internal sealed class PanelCitasService(
    ICitaRepositorio citas,
    IUnidadDeTrabajo unidadDeTrabajo,
    IUsuarioActual usuarioActual,
    INotificadorAgenda notificador,
    IRelojSalon reloj) : IPanelCitasService
{
    private const int DiasMaximosPorConsulta = 31;

    public async Task<IReadOnlyList<CitaPanelDto>> ListarAsync(
        DateOnly desde, DateOnly hasta, int? estilistaId, CancellationToken ct = default)
    {
        AsegurarSesion();
        if (hasta < desde)
            throw new ReglaNegocioException("La fecha final no puede ser anterior a la inicial.");
        if (hasta.DayNumber - desde.DayNumber >= DiasMaximosPorConsulta)
            throw new ReglaNegocioException($"Consulta como máximo {DiasMaximosPorConsulta} días a la vez.");

        // Un estilista siempre ve solo su agenda, aunque pida otro estilistaId.
        var filtroEstilista = usuarioActual.Rol == RolUsuario.Estilista
            ? usuarioActual.EstilistaId ?? throw new ProhibidoException("Tu cuenta no está asociada a un estilista.")
            : estilistaId;

        var lista = await citas.ListarEntreAsync(
            desde.ToDateTime(TimeOnly.MinValue), hasta.AddDays(1).ToDateTime(TimeOnly.MinValue), filtroEstilista, ct);
        return lista.Select(CitaPanelDto.Desde).ToList();
    }

    public Task<CitaPanelDto> MarcarCompletadaAsync(string codigo, CancellationToken ct = default) =>
        CambiarEstadoAsync(codigo, cita => cita.MarcarCompletada(reloj.Ahora), ct);

    public Task<CitaPanelDto> MarcarNoAsistioAsync(string codigo, CancellationToken ct = default) =>
        CambiarEstadoAsync(codigo, cita => cita.MarcarNoAsistio(reloj.Ahora), ct);

    public Task<CitaPanelDto> CancelarAsync(string codigo, CancellationToken ct = default) =>
        CambiarEstadoAsync(codigo, cita => cita.CancelarPorSalon(reloj.Ahora), ct);

    private async Task<CitaPanelDto> CambiarEstadoAsync(string codigo, Action<Cita> cambio, CancellationToken ct)
    {
        AsegurarSesion();
        var cita = CodigoCita.TryCrear(codigo, out var codigoValido)
            ? await citas.ObtenerPorCodigoAsync(codigoValido, ct)
            : null;
        if (cita is null)
            throw new NoEncontradoException(Mensajes.CitaNoEncontrada);
        if (usuarioActual.Rol == RolUsuario.Estilista && cita.EstilistaId != usuarioActual.EstilistaId)
            throw new ProhibidoException("Solo puedes gestionar tus propias citas.");

        cambio(cita);
        await unidadDeTrabajo.GuardarCambiosAsync(ct);

        var dto = CitaPanelDto.Desde(cita);
        await notificador.CitaActualizadaAsync(dto, ct);
        return dto;
    }

    private void AsegurarSesion()
    {
        if (!usuarioActual.EstaAutenticado)
            throw new NoAutorizadoException("Inicia sesión para usar el panel.");
    }
}

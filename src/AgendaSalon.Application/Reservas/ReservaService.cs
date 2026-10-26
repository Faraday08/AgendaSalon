using AgendaSalon.Application.Abstracciones;
using AgendaSalon.Application.Comun;
using AgendaSalon.Application.Disponibilidad;
using AgendaSalon.Domain.Citas;
using AgendaSalon.Domain.Clientes;
using AgendaSalon.Domain.Servicios;

namespace AgendaSalon.Application.Reservas;

public interface IReservaService
{
    Task<CitaDto> CrearAsync(CrearCitaRequest solicitud, CancellationToken ct = default);

    Task<CitaDto> ObtenerAsync(string codigo, CancellationToken ct = default);

    /// <summary>Pide el teléfono con que se reservó para confirmar que la cita es del cliente.</summary>
    Task<CitaDto> CancelarAsync(string codigo, string telefono, CancellationToken ct = default);
}

internal sealed class ReservaService(
    IServicioRepositorio servicios,
    IClienteRepositorio clientes,
    ICitaRepositorio citas,
    PlanificadorAgenda planificador,
    IUnidadDeTrabajo unidadDeTrabajo,
    IRelojSalon reloj) : IReservaService
{
    public async Task<CitaDto> CrearAsync(CrearCitaRequest solicitud, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(solicitud);

        // Validar los datos del cliente antes de abrir la transacción.
        var telefono = Telefono.Crear(solicitud.ClienteTelefono);
        var clienteNuevo = Cliente.Registrar(solicitud.ClienteNombre, telefono);

        try
        {
            return await unidadDeTrabajo.EjecutarEnTransaccionAsync(
                transaccionCt => ReservarAsync(solicitud, clienteNuevo, transaccionCt), ct);
        }
        catch (ConcurrenciaException)
        {
            throw new ConflictoException(Mensajes.HorarioNoDisponible);
        }
    }

    public async Task<CitaDto> ObtenerAsync(string codigo, CancellationToken ct = default) =>
        CitaDto.Desde(await BuscarCitaAsync(codigo, ct));

    public async Task<CitaDto> CancelarAsync(string codigo, string telefono, CancellationToken ct = default)
    {
        var cita = await BuscarCitaAsync(codigo, ct);
        cita.Cancelar(Telefono.Crear(telefono), reloj.Ahora);
        await unidadDeTrabajo.GuardarCambiosAsync(ct);
        return CitaDto.Desde(cita);
    }

    private async Task<CitaDto> ReservarAsync(CrearCitaRequest solicitud, Cliente clienteNuevo, CancellationToken ct)
    {
        var servicio = await servicios.ObtenerActivoAsync(solicitud.ServicioId, ct)
            ?? throw new NoEncontradoException(Mensajes.ServicioNoDisponible);

        var huecos = await planificador.BuscarHuecosAsync(
            servicio, DateOnly.FromDateTime(solicitud.Inicio), solicitud.EstilistaId, ct);
        var hueco = huecos.FirstOrDefault(h => h.Inicio == solicitud.Inicio)
            ?? throw new ConflictoException(Mensajes.HorarioNoDisponible);

        // Un cliente que ya reservó antes se reconoce por su teléfono y conserva su registro.
        var cliente = await clientes.ObtenerPorTelefonoAsync(clienteNuevo.Telefono, ct);
        if (cliente is null)
        {
            cliente = clienteNuevo;
            clientes.Agregar(cliente);
        }

        var cita = Cita.Reservar(
            await GenerarCodigoUnicoAsync(ct), cliente, servicio, hueco.Estilista, solicitud.Inicio, solicitud.Notas, reloj.Ahora);
        citas.Agregar(cita);
        await unidadDeTrabajo.GuardarCambiosAsync(ct);

        return CitaDto.Desde(cita);
    }

    private async Task<Cita> BuscarCitaAsync(string codigo, CancellationToken ct)
    {
        var cita = CodigoCita.TryCrear(codigo, out var codigoValido)
            ? await citas.ObtenerPorCodigoAsync(codigoValido, ct)
            : null;

        return cita ?? throw new NoEncontradoException(Mensajes.CitaNoEncontrada);
    }

    private async Task<CodigoCita> GenerarCodigoUnicoAsync(CancellationToken ct)
    {
        while (true)
        {
            var codigo = CodigoCita.Generar();
            if (!await citas.ExisteCodigoAsync(codigo, ct))
                return codigo;
        }
    }
}

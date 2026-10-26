using AgendaSalon.Domain.Estilistas;
using AgendaSalon.Domain.Servicios;
using Microsoft.EntityFrameworkCore;

namespace AgendaSalon.Infrastructure.Persistencia;

/// <summary>Datos de demostración del salón. Solo se cargan si la base está vacía.</summary>
internal static class DatosIniciales
{
    private static readonly DayOfWeek[] LunesASabado =
        [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday];

    public static async Task SembrarAsync(AgendaDbContext db, CancellationToken ct = default)
    {
        if (await db.Estilistas.AnyAsync(ct))
            return;

        var corte = CategoriaServicio.Crear("Corte");
        var barba = CategoriaServicio.Crear("Barba");
        var color = CategoriaServicio.Crear("Color");
        var peinado = CategoriaServicio.Crear("Peinado");
        var unas = CategoriaServicio.Crear("Uñas");
        var tratamiento = CategoriaServicio.Crear("Tratamiento");

        var corteDama = Servicio.Crear("Corte de dama", "Corte, lavado y secado.", corte, 45, 800m);
        var corteCaballero = Servicio.Crear("Corte de caballero", "Corte a tijera o máquina con terminación.", corte, 30, 500m);
        var recorteBarba = Servicio.Crear("Recorte de barba", "Perfilado y recorte con toalla caliente.", barba, 20, 300m);
        var corteYBarba = Servicio.Crear("Corte y barba", "Corte de caballero más recorte de barba.", barba, 45, 700m);
        var tinte = Servicio.Crear("Tinte completo", "Coloración de raíz a puntas con un solo tono.", color, 120, 2800m);
        var balayage = Servicio.Crear("Balayage", "Aclarado degradado hecho a mano alzada.", color, 180, 4500m);
        var blower = Servicio.Crear("Lavado y blower", "Lavado, secado y planchado con cepillo.", peinado, 45, 700m);
        var peinadoEvento = Servicio.Crear("Peinado para evento", "Recogido u ondas para bodas, graduaciones y fiestas.", peinado, 60, 1500m);
        var manicure = Servicio.Crear("Manicure", "Limpieza, forma y esmaltado tradicional.", unas, 45, 600m);
        var pedicure = Servicio.Crear("Pedicure", "Limpieza, exfoliación y esmaltado.", unas, 60, 800m);
        var keratina = Servicio.Crear("Tratamiento de keratina", "Alisado que reduce el frizz por varias semanas.", tratamiento, 150, 3500m);

        var carla = Estilista.Contratar("Carla Méndez", "Colorista");
        Asignar(carla, corteDama, tinte, balayage, blower, keratina);
        AgregarJornadas(carla, LunesASabado, new TimeOnly(9, 0), new TimeOnly(18, 0));

        var luis = Estilista.Contratar("Luis Peña", "Barbero");
        Asignar(luis, corteCaballero, recorteBarba, corteYBarba);
        AgregarJornadas(luis, LunesASabado.Except([DayOfWeek.Monday]), new TimeOnly(10, 0), new TimeOnly(20, 0));
        luis.AgregarJornada(DayOfWeek.Sunday, new TimeOnly(9, 0), new TimeOnly(13, 0));

        var yaritza = Estilista.Contratar("Yaritza Ramírez", "Uñas y peinados");
        Asignar(yaritza, corteDama, blower, peinadoEvento, manicure, pedicure);
        AgregarJornadas(yaritza, LunesASabado, new TimeOnly(9, 0), new TimeOnly(17, 0));

        db.Estilistas.AddRange(carla, luis, yaritza);
        await db.SaveChangesAsync(ct);
    }

    private static void Asignar(Estilista estilista, params Servicio[] servicios)
    {
        foreach (var servicio in servicios)
            estilista.AsignarServicio(servicio);
    }

    private static void AgregarJornadas(Estilista estilista, IEnumerable<DayOfWeek> dias, TimeOnly inicio, TimeOnly fin)
    {
        foreach (var dia in dias)
            estilista.AgregarJornada(dia, inicio, fin);
    }
}

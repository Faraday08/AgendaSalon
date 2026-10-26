namespace AgendaSalon.Application.Orientacion;

internal sealed record RecomendacionDef(string Motivo, params string[] Servicios);

/// <summary>Una opción lleva a otra pregunta o termina en una recomendación.</summary>
internal sealed record OpcionDef(string Texto, string? Siguiente = null, RecomendacionDef? Recomendacion = null);

internal sealed record PreguntaDef(string Id, string Texto, params OpcionDef[] Opciones);

/// <summary>
/// Regla de texto libre: se cumple si el texto tiene al menos una palabra de cada grupo.
/// Las palabras son raíces sin tildes ("cort" vale para corte, cortar, cortarme).
/// </summary>
internal sealed record ReglaPalabras(string[][] Grupos, string? Pregunta = null, RecomendacionDef? Recomendacion = null);

/// <summary>
/// Conocimiento del salón para orientar al cliente, escrito como datos. Los servicios se nombran
/// como en el catálogo; si alguno deja de existir o se desactiva, simplemente no se recomienda.
/// </summary>
internal static class ReglasOrientacion
{
    public const string Inicio = "inicio";

    private static readonly RecomendacionDef CorteDama = new("Incluye lavado y secado, así sales lista.", "Corte de dama");
    private static readonly RecomendacionDef CorteCaballero = new("Corte a tijera o máquina con terminación.", "Corte de caballero");
    private static readonly RecomendacionDef CorteYBarba = new("Te sale mejor que pedirlos por separado y quedas listo en una sola cita.", "Corte y barba");
    private static readonly RecomendacionDef Barba = new("Perfilado y recorte con toalla caliente.", "Recorte de barba");
    private static readonly RecomendacionDef Tinte = new("Un solo tono de raíz a puntas; es lo indicado para cubrir canas.", "Tinte completo");
    private static readonly RecomendacionDef Balayage = new("Aclara a mano alzada y deja un degradado natural, sin la línea marcada de las mechas tradicionales.", "Balayage");
    private static readonly RecomendacionDef Evento = new("Un peinado pensado para que te dure toda la celebración. Si solo quieres el cabello liso y con volumen, el blower es más rápido.", "Peinado para evento", "Lavado y blower");
    private static readonly RecomendacionDef Keratina = new("Reduce el frizz y deja el cabello manejable por varias semanas.", "Tratamiento de keratina");
    private static readonly RecomendacionDef Blower = new("Lavado, secado y planchado con cepillo.", "Lavado y blower");
    private static readonly RecomendacionDef Manicure = new("Limpieza, forma y esmaltado de las manos.", "Manicure");
    private static readonly RecomendacionDef Pedicure = new("Limpieza, exfoliación y esmaltado de los pies.", "Pedicure");
    private static readonly RecomendacionDef ManosYPies = new("Puedes reservar uno después del otro el mismo día.", "Manicure", "Pedicure");

    public static readonly IReadOnlyList<PreguntaDef> Preguntas =
    [
        new(Inicio, "¿Qué te gustaría hacerte?",
            new("Cortarme el cabello", Siguiente: "corte"),
            new("Arreglarme la barba", Siguiente: "barba"),
            new("Cambiar el color", Siguiente: "color"),
            new("Peinarme para un evento", Recomendacion: Evento),
            new("Quitar el frizz o alisar", Recomendacion: Keratina),
            new("Solo lavado y secado", Recomendacion: Blower),
            new("Arreglarme las uñas", Siguiente: "unas")),

        new("corte", "¿Para quién es el corte?",
            new("Para dama", Recomendacion: CorteDama),
            new("Para caballero", Siguiente: "corte-barba")),

        new("corte-barba", "¿También quieres arreglarte la barba?",
            new("Sí, corte y barba", Recomendacion: CorteYBarba),
            new("No, solo el corte", Recomendacion: CorteCaballero)),

        new("barba", "¿Quieres aprovechar y cortarte el cabello también?",
            new("Sí, corte y barba", Recomendacion: CorteYBarba),
            new("No, solo la barba", Recomendacion: Barba)),

        new("color", "¿Qué estás buscando?",
            new("Un solo tono o cubrir canas", Recomendacion: Tinte),
            new("Mechas o un efecto degradado natural", Recomendacion: Balayage),
            new("No estoy seguro", Recomendacion: new RecomendacionDef(
                "Si quieres un cambio parejo o tapar canas, elige el tinte; si prefieres luz y un acabado natural, el balayage.",
                "Tinte completo", "Balayage"))),

        new("unas", "¿Manos, pies o ambos?",
            new("Manos", Recomendacion: Manicure),
            new("Pies", Recomendacion: Pedicure),
            new("Ambos", Recomendacion: ManosYPies))
    ];

    /// <summary>Se evalúan en orden: van primero las más específicas. Gana la que más grupos cumple.</summary>
    public static readonly IReadOnlyList<ReglaPalabras> Palabras =
    [
        new([["barba", "bigote"], ["cort", "pelar", "pelad", "recort"]], Recomendacion: CorteYBarba),
        new([["manicur", "manos"], ["pedicur", "pies"]], Recomendacion: ManosYPies),
        new([["cort"], ["dama", "mujer", "nena", "hija"]], Recomendacion: CorteDama),
        new([["cort", "pelar", "pelad"], ["caballero", "hombre", "varon", "nino", "hijo"]], Pregunta: "corte-barba"),
        new([["boda", "casar", "caso", "fiesta", "graduacion", "quince", "evento", "cumpleanos", "recogido", "ondas", "peinad"]], Recomendacion: Evento),
        new([["frizz", "encresp", "esponj", "alisa", "liso", "keratina", "rebelde", "maltrat", "reseco"]], Recomendacion: Keratina),
        new([["mecha", "balayage", "ilumin", "rubi", "degrad", "reflejo"]], Recomendacion: Balayage),
        new([["canas", "tint", "color", "pintar", "tenir", "teni"]], Pregunta: "color"),
        new([["barba", "bigote", "perfil"]], Pregunta: "barba"),
        new([["manicur", "manos", "esmalt", "acrilic", "gel"]], Recomendacion: Manicure),
        new([["pedicur", "pies"]], Recomendacion: Pedicure),
        new([["unas"]], Pregunta: "unas"),
        new([["blower", "secado", "lavar", "lavado", "planch"]], Recomendacion: Blower),
        new([["cort", "pelar", "pelad", "recort"]], Pregunta: "corte")
    ];
}

namespace ETL.Opiniones.Models;

public sealed record OpinionRecord(
    string Fuente,
    string IdOrigen,
    string Cliente,
    string Comentario,
    DateTimeOffset? FechaOriginal,
    DateTimeOffset FechaExtraccionUtc);

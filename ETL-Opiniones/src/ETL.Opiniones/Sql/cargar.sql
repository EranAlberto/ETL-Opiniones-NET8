INSERT OR IGNORE INTO DimFuente(Nombre) VALUES ($fuente);
INSERT OR IGNORE INTO DimCliente(Nombre, Fuente) VALUES ($cliente, $fuente);
INSERT OR IGNORE INTO DimFecha(FechaId, Fecha, Anio, Mes, Dia)
SELECT CAST(REPLACE($fecha, '-', '') AS INTEGER), $fecha,
 CAST(substr($fecha,1,4) AS INTEGER), CAST(substr($fecha,6,2) AS INTEGER), CAST(substr($fecha,9,2) AS INTEGER)
WHERE $fecha IS NOT NULL;

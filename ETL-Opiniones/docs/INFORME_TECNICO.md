# Creacion de la arquitectura y desarrollo del proceso de extraccion

**Proyecto:** Analisis de opiniones y encuestas

**Asignatura:** Electiva 1 - Big Data

**Estudiante:** ____________________

**Matricula:** ____________________

**Fecha:** Noviembre de 2026

## 1. Introduccion

Este proyecto organiza la extraccion de opiniones de clientes procedentes de varias fuentes. El objetivo de la actividad es construir la base de la arquitectura y trabajar la fase E de ETL, sin abordar todavia la transformacion ni la carga en la base analitica.

## 2. Arquitectura propuesta

Se utiliza un Worker Service de .NET 8 para coordinar tres extractores independientes. CsvExtractor lee encuestas desde un archivo CSV; DatabaseExtractor consulta resenas de una base SQLite; ApiExtractor solicita comentarios a una API REST. Todos usan el contrato IExtractor y entregan registros con la misma estructura a StagingStore. Los resultados se guardan en archivos JSONL temporales separados por fuente.

La base analitica y el dashboard se representan como siguientes fases previstas. No se simula que esten ya desarrollados.

**Diagrama de arquitectura:** ver `arquitectura.png`.

## 3. Flujo de extraccion

1. El programa lee las opciones de appsettings.json.
2. En modo demostracion, se crea una base SQLite local con cinco resenas.
3. Worker ejecuta las tres fuentes de manera concurrente.
4. Cada extractor valida los datos basicos y produce registros con fuente, identificador, cliente, comentario, fecha original cuando existe y fecha de extraccion.
5. StagingStore escribe un archivo JSONL por fuente. Solo reemplaza el archivo previo al terminar correctamente su extraccion.
6. ILogger registra cantidad de registros, tiempo y errores. Si alguna fuente falla, las otras siguen su ejecucion.

**Diagrama de flujo:** ver `flujo_extraccion.png`.

## 4. Decisiones tecnicas y atributos de calidad

- **Rendimiento:** lectura secuencial por streaming y operaciones asincronas donde la API las soporta, con ejecucion concurrente entre fuentes y medicion mediante Stopwatch. En SQLite, el proveedor puede ejecutar internamente operaciones sin I/O asincrona real.
- **Escalabilidad:** la interfaz IExtractor permite incorporar nuevas fuentes sin reescribir el coordinador; cada fuente crea su propio archivo staging.
- **Seguridad:** no se incluye ninguna clave en el repositorio. Los parametros estan centralizados y las credenciales reales deben venir de variables de entorno o un almacen seguro. Se comprueba la respuesta HTTP antes de procesarla.
- **Mantenibilidad:** se separan contratos, modelos, servicios de almacenamiento, configuracion y extractores. Cada clase tiene una responsabilidad principal.

## 5. Evidencias del codigo

Archivos principales: `Worker.cs`, `Program.cs`, `IExtractor.cs`, `CsvExtractor.cs`, `DatabaseExtractor.cs`, `ApiExtractor.cs`, `StagingStore.cs` y `DemoDatabaseSeeder.cs`.

**Evidencia de compilacion y ejecucion:** pendiente de insertar una captura autentica de la terminal tras `dotnet run`.

**Evidencia de los resultados:** pendiente de insertar una captura autentica de `Staging/csv.jsonl`, `Staging/database.jsonl` y `Staging/api.jsonl`.

## 6. Conclusiones

Se definio una arquitectura modular y se programo la fase de extraccion para CSV, base de datos relacional y API REST. Con esto se establece una base para agregar luego transformacion, carga analitica y visualizacion, sin mezclar esas etapas con el alcance actual.

**Estado de validacion:** el proyecto se preparo para .NET 8, pero no se ha compilado ni ejecutado en este entorno; se requiere verificarlo en Visual Studio o con el SDK de .NET 8. El servicio REST de demostracion necesita acceso a Internet.

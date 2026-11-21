# ETL de opiniones - Extraccion con .NET 8

Proyecto academico para la actividad **Creacion de la Arquitectura y Desarrollo del Proceso de Extraccion**. Implementa exclusivamente la fase **E** de ETL a partir de tres fuentes: encuestas CSV, resenas en SQLite y comentarios de una API REST.

## Requisitos

- SDK de .NET 8 instalado.
- Internet para restaurar paquetes NuGet y consultar la API REST de demostracion.
- No requiere SQL Server ni Docker.

## Ejecucion

Abre una terminal dentro de `src/ETL.Opiniones` y escribe:

```powershell
dotnet restore
dotnet run
```

Al ejecutar por primera vez, se crea `Data/resenas.db` con 5 resenas de ejemplo (solo si la tabla esta vacia). Se leen 5 encuestas del CSV, 5 resenas de la BD y, si hay conexion, hasta 5 comentarios de la API JSONPlaceholder (`postId=1`). El resultado queda en:

- `Staging/csv.jsonl`
- `Staging/database.jsonl`
- `Staging/api.jsonl`

Cada linea del archivo JSONL representa un registro. No se desarrolla la fase T o L en esta actividad. La base de datos analitica y el dashboard aparecen en el diagrama como **componentes futuros**, no implementados.

## Estructura

- `Program.cs`: configuracion de servicios y dependencias.
- `Worker.cs`: coordina las tres extracciones en paralelo, registra resultados y termina la ejecucion.
- `Contracts/IExtractor.cs`: contrato unico de las fuentes.
- `Extractors/*`: lectura individual del CSV, SQLite y API.
- `Services/StagingStore.cs`: salida temporal JSONL y reemplazo seguro de archivos.
- `Services/DemoDatabaseSeeder.cs`: crea datos de ejemplo en SQLite.
- `Configuration/ExtractionOptions.cs`: opciones del proceso.
- `docs/`: diagramas y documento tecnico.

## Configuracion

Modifica `src/ETL.Opiniones/appsettings.json` para usar rutas, base de datos y URL propias. **No almacenes contrasenas ni tokens en este archivo**. Para secretos, emplea variables de entorno como `Extraction__ApiUrl` o un gestor de secretos. Cambia `SeedDemoDatabase` a `false` cuando utilices una base de datos real ya preparada.

La API utilizada es un servicio publico de pruebas; puede fallar si no hay Internet. Si falla, las otras dos fuentes siguen ejecutandose y se conserva su staging. Los errores se registran por `ILogger` y se retorna codigo de salida 1 para indicar extraccion parcial. El codigo fuente no contiene credenciales.

## Evidencias recomendadas

1. Captura de `dotnet run` con logs y duraciones por fuente.
2. Captura de `Staging/` con los tres archivos `.jsonl` generados.
3. Captura de uno de los archivos mostrando los registros normalizados.
4. Captura del repositorio publicado en GitHub.

No se incluyen capturas de una ejecucion real porque esta entrega se genero en un entorno sin SDK de .NET instalado; la compilacion y el comportamiento de la API deben verificarse localmente.

## Diagramas

`docs/arquitectura.png`, `docs/flujo_extraccion.png`. Sus archivos editables `*.dot` se pueden abrir con Graphviz. Tambien se entregan por separado para insertar en una presentacion o en el informe.

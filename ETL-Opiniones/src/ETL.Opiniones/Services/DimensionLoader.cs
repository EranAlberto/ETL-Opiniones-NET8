using System.Text.Json;
using ETL.Opiniones.Configuration;
using ETL.Opiniones.Models;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ETL.Opiniones.Services;

public sealed class DimensionLoader(IOptions<ExtractionOptions> options,
    IHostEnvironment environment, ILogger<DimensionLoader> logger)
{
    public async Task LoadAsync(CancellationToken token)
    {
        string root = environment.ContentRootPath;
        string staging = Path.Combine(root, options.Value.StagingPath);
        string path = Path.Combine(root, "Data", "warehouse.db");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        string schema = await File.ReadAllTextAsync(Path.Combine(root, "Sql", "dimensiones.sql"), token);
        string load = await File.ReadAllTextAsync(Path.Combine(root, "Sql", "cargar.sql"), token);
        await using var db = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = path }.ToString());
        await db.OpenAsync(token);
        using var tx = db.BeginTransaction();
        using var command = db.CreateCommand();
        command.Transaction = tx;
        command.CommandText = schema;
        await command.ExecuteNonQueryAsync(token);
        int total = 0;
        foreach (string file in new[] { "csv.jsonl", "database.jsonl", "api.jsonl" })
        {
            foreach (string line in await File.ReadAllLinesAsync(Path.Combine(staging, file), token))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                var row = JsonSerializer.Deserialize<OpinionRecord>(line, new JsonSerializerOptions(JsonSerializerDefaults.Web))
                    ?? throw new InvalidDataException("Registro JSONL invalido");
                command.CommandText = load;
                command.Parameters.Clear();
                command.Parameters.AddWithValue("$fuente", row.Fuente.Trim());
                command.Parameters.AddWithValue("$cliente", string.IsNullOrWhiteSpace(row.Cliente) ? "Anonimo" : row.Cliente.Trim());
                command.Parameters.AddWithValue("$fecha", (object?)row.FechaOriginal?.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture) ?? DBNull.Value);
                await command.ExecuteNonQueryAsync(token);
                total++;
            }
        }
        tx.Commit();
        logger.LogInformation("Carga dimensional completada: {Total} registros procesados.", total);
        command.Transaction = null;
        command.Parameters.Clear();
        foreach (string table in new[] { "DimFuente", "DimCliente", "DimFecha" })
        {
            command.CommandText = $"SELECT COUNT(*) FROM {table}";
            logger.LogInformation("{Table}: {Count} filas", table, await command.ExecuteScalarAsync(token));
        }
        logger.LogInformation("Data Warehouse: {Path}", path);
    }
}

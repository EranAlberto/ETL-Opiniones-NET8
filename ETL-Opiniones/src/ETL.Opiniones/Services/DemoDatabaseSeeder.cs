using ETL.Opiniones.Configuration;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace ETL.Opiniones.Services;

public sealed class DemoDatabaseSeeder
{
    private readonly ExtractionOptions _options;
    private readonly IHostEnvironment _environment;

    public DemoDatabaseSeeder(IOptions<ExtractionOptions> options, IHostEnvironment environment)
    {
        _options = options.Value;
        _environment = environment;
    }

    public async Task SeedIfNeededAsync(CancellationToken cancellationToken)
    {
        string path = Path.GetFullPath(Path.Combine(
            _environment.ContentRootPath, _options.DatabasePath));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await using var connection = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = path
        }.ToString());
        await connection.OpenAsync(cancellationToken);

        await using var create = connection.CreateCommand();
        create.CommandText = """
            CREATE TABLE IF NOT EXISTS Resenas (
                Id INTEGER PRIMARY KEY,
                Cliente TEXT NOT NULL,
                Comentario TEXT NOT NULL,
                Fecha TEXT NOT NULL
            );
            """;
        await create.ExecuteNonQueryAsync(cancellationToken);

        await using var count = connection.CreateCommand();
        count.CommandText = "SELECT COUNT(*) FROM Resenas;";
        long existing = (long)(await count.ExecuteScalarAsync(cancellationToken) ?? 0L);
        if (existing > 0) return;

        var reviews = new (string Cliente, string Comentario, string Fecha)[]
        {
            ("Claudia", "El servicio fue rapido y claro.", "2026-11-01T10:00:00Z"),
            ("Jose", "La aplicacion necesita mejorar su menu.", "2026-11-02T11:30:00Z"),
            ("Rosa", "La atencion fue excelente.", "2026-11-03T09:15:00Z"),
            ("Miguel", "El tiempo de respuesta puede mejorar.", "2026-11-04T16:45:00Z"),
            ("Ana", "Volveria a utilizar el servicio.", "2026-11-05T08:00:00Z")
        };
        foreach (var row in reviews)
        {
            await using var insert = connection.CreateCommand();
            insert.CommandText = "INSERT INTO Resenas (Cliente, Comentario, Fecha) VALUES ($c, $t, $f);";
            insert.Parameters.AddWithValue("$c", row.Cliente);
            insert.Parameters.AddWithValue("$t", row.Comentario);
            insert.Parameters.AddWithValue("$f", row.Fecha);
            await insert.ExecuteNonQueryAsync(cancellationToken);
        }
    }
}

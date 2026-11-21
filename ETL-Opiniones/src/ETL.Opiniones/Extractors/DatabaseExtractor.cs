using System.Globalization;
using System.Runtime.CompilerServices;
using ETL.Opiniones.Configuration;
using ETL.Opiniones.Contracts;
using ETL.Opiniones.Models;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace ETL.Opiniones.Extractors;

public sealed class DatabaseExtractor : IExtractor
{
    private readonly ExtractionOptions _options;
    private readonly IHostEnvironment _environment;
    public string SourceName => "database";

    public DatabaseExtractor(IOptions<ExtractionOptions> options,
        IHostEnvironment environment)
    {
        _options = options.Value;
        _environment = environment;
    }

    public async IAsyncEnumerable<OpinionRecord> ReadAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        string path = Path.GetFullPath(Path.Combine(
            _environment.ContentRootPath, _options.DatabasePath));
        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = path,
            Mode = SqliteOpenMode.ReadOnly
        }.ToString();

        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, Cliente, Comentario, Fecha FROM Resenas ORDER BY Id;";
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            string id = reader.GetInt64(0).ToString(CultureInfo.InvariantCulture);
            string cliente = reader.GetString(1);
            string comentario = reader.GetString(2);
            string fecha = reader.GetString(3);
            if (!DateTimeOffset.TryParse(fecha, CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var parsedDate)) continue;
            yield return new OpinionRecord("Base de datos", id, cliente, comentario,
                parsedDate, DateTimeOffset.UtcNow);
        }
    }
}

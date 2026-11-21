using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Runtime.CompilerServices;
using CsvHelper;
using ETL.Opiniones.Configuration;
using ETL.Opiniones.Contracts;
using ETL.Opiniones.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace ETL.Opiniones.Extractors;

public sealed class CsvExtractor : IExtractor
{
    private readonly ExtractionOptions _options;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<CsvExtractor> _logger;
    public string SourceName => "csv";

    public CsvExtractor(IOptions<ExtractionOptions> options,
        IHostEnvironment environment, ILogger<CsvExtractor> logger)
    {
        _options = options.Value;
        _environment = environment;
        _logger = logger;
    }

    public async IAsyncEnumerable<OpinionRecord> ReadAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        string path = Path.GetFullPath(Path.Combine(
            _environment.ContentRootPath, _options.CsvPath));
        using var file = new StreamReader(path);
        using var csv = new CsvReader(file, CultureInfo.InvariantCulture);
        if (!await csv.ReadAsync()) yield break;
        csv.ReadHeader();

        while (await csv.ReadAsync())
        {
            cancellationToken.ThrowIfCancellationRequested();
            string id = csv.GetField("Id") ?? "";
            string cliente = csv.GetField("Cliente") ?? "";
            string comentario = csv.GetField("Comentario") ?? "";
            string fecha = csv.GetField("Fecha") ?? "";
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(comentario))
            {
                _logger.LogWarning("Fila CSV {Row} omitida: Id o Comentario vacio.", csv.Context.Parser.Row);
                continue;
            }
            if (!DateTimeOffset.TryParse(fecha, CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var parsedDate))
            {
                _logger.LogWarning("Fila CSV {Row} omitida: fecha invalida.", csv.Context.Parser.Row);
                continue;
            }
            yield return new OpinionRecord("CSV", id, cliente, comentario,
                parsedDate, DateTimeOffset.UtcNow);
        }
    }
}

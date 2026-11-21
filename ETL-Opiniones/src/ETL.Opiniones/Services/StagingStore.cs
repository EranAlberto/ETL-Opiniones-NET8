using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;
using ETL.Opiniones.Configuration;
using ETL.Opiniones.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace ETL.Opiniones.Services;

public sealed class StagingStore
{
    private readonly ExtractionOptions _options;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<StagingStore> _logger;

    public StagingStore(IOptions<ExtractionOptions> options,
        IHostEnvironment environment, ILogger<StagingStore> logger)
    {
        _options = options.Value;
        _environment = environment;
        _logger = logger;
    }

    public async Task<int> SaveAsync(string sourceName,
        IAsyncEnumerable<OpinionRecord> records, CancellationToken cancellationToken)
    {
        string directory = Path.GetFullPath(Path.Combine(
            _environment.ContentRootPath, _options.StagingPath));
        Directory.CreateDirectory(directory);

        string destination = Path.Combine(directory, $"{sourceName}.jsonl");
        string tempFile = destination + "." + Guid.NewGuid().ToString("N") + ".tmp";
        int count = 0;
        try
        {
            await using (var stream = new FileStream(tempFile, FileMode.CreateNew,
                FileAccess.Write, FileShare.None, 81920, FileOptions.Asynchronous))
            await using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
            {
                await foreach (var record in records.WithCancellation(cancellationToken))
                {
                    string line = JsonSerializer.Serialize(record,
                        new JsonSerializerOptions(JsonSerializerDefaults.Web));
                    await writer.WriteLineAsync(line.AsMemory(), cancellationToken);
                    count++;
                }
                await writer.FlushAsync(cancellationToken);
            }

            // Reemplazo atomico cuando el archivo temporal ya esta completo.
            File.Move(tempFile, destination, overwrite: true);
            _logger.LogInformation("Staging {Source}: {Count} registros en {File}",
                sourceName, count, destination);
            return count;
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }
}

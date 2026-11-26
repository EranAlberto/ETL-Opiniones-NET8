using Microsoft.Extensions.Logging;
using System.Diagnostics;
using ETL.Opiniones.Configuration;
using ETL.Opiniones.Contracts;
using ETL.Opiniones.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace ETL.Opiniones;

public sealed class Worker : BackgroundService
{
    private readonly IReadOnlyList<IExtractor> _extractors;
    private readonly StagingStore _staging;
    private readonly DimensionLoader _loader;
    private readonly DemoDatabaseSeeder _seeder;
    private readonly ExtractionOptions _options;
    private readonly ILogger<Worker> _logger;
    private readonly IHostApplicationLifetime _lifetime;

    public Worker(IEnumerable<IExtractor> extractors,
        StagingStore staging,
        DimensionLoader loader,
        DemoDatabaseSeeder seeder,
        IOptions<ExtractionOptions> options,
        ILogger<Worker> logger,
        IHostApplicationLifetime lifetime)
    {
        _extractors = extractors.ToList();
        _staging = staging;
        _loader = loader;
        _seeder = seeder;
        _options = options.Value;
        _logger = logger;
        _lifetime = lifetime;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // El Worker ejecuta una sola corrida para esta actividad.
        try
        {
            if (_options.SeedDemoDatabase)
            {
                await _seeder.SeedIfNeededAsync(stoppingToken);
                _logger.LogInformation("Base de datos de demostracion disponible.");
            }
            _logger.LogInformation("Inicio del proceso de extraccion ETL.");
            var jobs = _extractors.Select(extractor => RunSourceAsync(extractor, stoppingToken));
            bool[] results = await Task.WhenAll(jobs);
            if (results.Any(ok => !ok))
            {
                Environment.ExitCode = 1;
                _logger.LogWarning("Finalizado con fuentes que presentaron errores.");
            }
            else
            {
                _logger.LogInformation("Todas las extracciones finalizaron correctamente.");
                await _loader.LoadAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogWarning("Ejecucion cancelada.");
        }
        catch (Exception ex)
        {
            Environment.ExitCode = 1;
            _logger.LogError(ex, "Error general del Worker.");
        }
        finally
        {
            _lifetime.StopApplication();
        }
    }

    private async Task<bool> RunSourceAsync(IExtractor extractor, CancellationToken token)
    {
        var watch = Stopwatch.StartNew();
        try
        {
            int count = await _staging.SaveAsync(extractor.SourceName,
                extractor.ReadAsync(token), token);
            watch.Stop();
            _logger.LogInformation("{Source}: {Count} registros, {Ms} ms.",
                extractor.SourceName, count, watch.ElapsedMilliseconds);
            return true;
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            _logger.LogWarning("Extraccion {Source} cancelada.", extractor.SourceName);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fallo al extraer desde {Source}.", extractor.SourceName);
            return false;
        }
    }
}

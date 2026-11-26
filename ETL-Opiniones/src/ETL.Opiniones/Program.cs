using ETL.Opiniones;
using ETL.Opiniones.Configuration;
using ETL.Opiniones.Contracts;
using ETL.Opiniones.Extractors;
using ETL.Opiniones.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<ExtractionOptions>(builder.Configuration.GetSection("Extraction"));
builder.Services.AddHttpClient("comentarios-api", client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.UserAgent.ParseAdd("ETL-Opiniones-Academico/1.0");
});

builder.Services.AddSingleton<StagingStore>();
builder.Services.AddSingleton<DimensionLoader>();
builder.Services.AddSingleton<DemoDatabaseSeeder>();
builder.Services.AddTransient<IExtractor, CsvExtractor>();
builder.Services.AddTransient<IExtractor, DatabaseExtractor>();
builder.Services.AddTransient<IExtractor, ApiExtractor>();
builder.Services.AddHostedService<Worker>();

await builder.Build().RunAsync();

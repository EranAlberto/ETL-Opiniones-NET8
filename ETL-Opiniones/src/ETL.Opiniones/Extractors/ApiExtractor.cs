using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using ETL.Opiniones.Configuration;
using ETL.Opiniones.Contracts;
using ETL.Opiniones.Models;
using Microsoft.Extensions.Options;

namespace ETL.Opiniones.Extractors;

public sealed class ApiExtractor : IExtractor
{
    private readonly IHttpClientFactory _clientFactory;
    private readonly ExtractionOptions _options;
    public string SourceName => "api";

    public ApiExtractor(IHttpClientFactory clientFactory,
        IOptions<ExtractionOptions> options)
    {
        _clientFactory = clientFactory;
        _options = options.Value;
    }

    public async IAsyncEnumerable<OpinionRecord> ReadAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var client = _clientFactory.CreateClient("comentarios-api");
        using var response = await client.GetAsync(_options.ApiUrl,
            HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);

        await foreach (var item in JsonSerializer.DeserializeAsyncEnumerable<ApiComment>(
            stream, new JsonSerializerOptions(JsonSerializerDefaults.Web), cancellationToken))
        {
            if (item is null || item.Id <= 0 || string.IsNullOrWhiteSpace(item.Body))
                continue;
            yield return new OpinionRecord("API REST", item.Id.ToString(),
                item.Name ?? "Anonimo", item.Body, null, DateTimeOffset.UtcNow);
        }
    }

    private sealed class ApiComment
    {
        [JsonPropertyName("id")] public int Id { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("body")] public string? Body { get; set; }
    }
}

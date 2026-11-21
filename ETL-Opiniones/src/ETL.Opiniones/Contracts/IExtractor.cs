using ETL.Opiniones.Models;

namespace ETL.Opiniones.Contracts;

public interface IExtractor
{
    string SourceName { get; }
    IAsyncEnumerable<OpinionRecord> ReadAsync(CancellationToken cancellationToken);
}

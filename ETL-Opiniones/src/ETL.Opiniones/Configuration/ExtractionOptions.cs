namespace ETL.Opiniones.Configuration;

public sealed class ExtractionOptions
{
    public string CsvPath { get; set; } = "Data/encuestas.csv";
    public string DatabasePath { get; set; } = "Data/resenas.db";
    public string ApiUrl { get; set; } = "https://jsonplaceholder.typicode.com/comments?postId=1";
    public string StagingPath { get; set; } = "Staging";
    public bool SeedDemoDatabase { get; set; } = true;
}

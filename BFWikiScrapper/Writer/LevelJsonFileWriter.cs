using System.Text.Json;
using System.Text.Json.Serialization;
using BFWikiScrapper.Model;

namespace BFWikiScrapper.Writer;

public sealed class LevelJsonFileWriter : IFileWriter<LevelData>
{
    private readonly static JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public string FileExtension => ".json";

    public async ValueTask WriteAsync(string filePath, List<LevelData> levels, CancellationToken cancellationToken = default)
    {
        await using var fileStream = File.Create(filePath);
        await JsonSerializer.SerializeAsync(fileStream, levels, Options, cancellationToken);
    }
}
using System.Text.Json;
using System.Text.Json.Serialization;
using BFWikiScrapper.Interface;
using BFWikiScrapper.Model;

namespace BFWikiScrapper;

/// <summary>
///     JSON writer for unit data using System.Text.Json.
/// </summary>
public sealed class JsonFileWriter : IFileWriter
{
    private readonly static JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public string FileExtension => ".json";

    public async ValueTask WriteAsync(string filePath, List<UnitData> units, CancellationToken cancellationToken = default)
    {
        await using var fileStream = File.Create(filePath);
        await JsonSerializer.SerializeAsync(fileStream, units, Options, cancellationToken);
    }
}
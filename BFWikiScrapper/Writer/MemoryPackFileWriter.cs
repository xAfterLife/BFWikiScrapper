using BFWikiScrapper.Interface;
using BFWikiScrapper.Model;
using MemoryPack;

namespace BFWikiScrapper.Writer;

/// <summary>
///     MemoryPack writer for compact binary serialization of unit data.
/// </summary>
public sealed class MemoryPackFileWriter : IFileWriter
{
    public string FileExtension => ".mp";

    public async ValueTask WriteAsync(string filePath, List<UnitData> units, CancellationToken cancellationToken = default)
    {
        var bytes = MemoryPackSerializer.Serialize(units);
        await File.WriteAllBytesAsync(filePath, bytes, cancellationToken);
    }
}
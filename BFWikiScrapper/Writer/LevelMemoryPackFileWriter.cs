using BFWikiScrapper.Model;
using MemoryPack;

namespace BFWikiScrapper.Writer;

public sealed class LevelMemoryPackFileWriter : IFileWriter<LevelData>
{
    public string FileExtension => ".mp";

    public async ValueTask WriteAsync(string filePath, List<LevelData> levels, CancellationToken cancellationToken = default)
    {
        var bytes = MemoryPackSerializer.Serialize(levels);
        await File.WriteAllBytesAsync(filePath, bytes, cancellationToken);
    }
}
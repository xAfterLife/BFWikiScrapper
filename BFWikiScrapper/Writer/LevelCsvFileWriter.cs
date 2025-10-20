using BFWikiScrapper.Model;

namespace BFWikiScrapper.Writer;

public sealed class LevelCsvFileWriter : IFileWriter<LevelData>
{
    public string FileExtension => ".csv";

    public async ValueTask WriteAsync(string filePath, List<LevelData> levels, CancellationToken cancellationToken = default)
    {
        await using var writer = new StreamWriter(File.OpenWrite(filePath), bufferSize: 65536);
        await writer.WriteLineAsync("Level;XpRequired");

        foreach ( var level in levels )
        {
            await writer.WriteLineAsync($"{level.Level};{level.XpRequired}");
        }
    }
}
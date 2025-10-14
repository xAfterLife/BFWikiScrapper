using BFWikiScrapper.Interface;
using BFWikiScrapper.Model;

namespace BFWikiScrapper;

/// <summary>
///     CSV writer for unit data.
/// </summary>
public sealed class CsvFileWriter : IFileWriter
{
    public string FileExtension => ".csv";

    public async ValueTask WriteAsync(string filePath, List<UnitData> units, CancellationToken cancellationToken = default)
    {
        await using var writer = new StreamWriter(File.OpenWrite(filePath), bufferSize: 65536);
        await writer.WriteLineAsync("UnitID;Name;Rarity;UnitDataID;ImageUrl");

        foreach ( var line in units.Select(unit => $"{unit.UnitId};{unit.Name};{unit.Rarity};{unit.UnitDataId};{unit.ImageUrl}") )
        {
            await writer.WriteLineAsync(line);
        }
    }
}
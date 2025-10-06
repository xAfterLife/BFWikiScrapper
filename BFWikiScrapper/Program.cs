namespace BFWikiScrapper;

public static class Program
{
    public static async Task Main()
    {
        using var scraper = new WikiScraper();
        Console.WriteLine("Starting Brave Frontier Wiki scrape...");

        var units = await scraper.ScrapeUnitsAsync(maxConcurrency: 16);
        Console.WriteLine($"\nScraped {units.Count} units");

        const string outputPath = "brave_frontier_units.csv";
        await WriteCsvAsync(outputPath, units);
        Console.WriteLine($"Results written to {outputPath}");
    }

    private static async Task WriteCsvAsync(string path, List<UnitData> units)
    {
        await using var writer = new StreamWriter(path);
        await writer.WriteLineAsync("UnitID;Name;Rarity;UnitDataID;ImageUrl");
        foreach ( var unit in units )
        {
            await writer.WriteLineAsync($"{unit.UnitId};{unit.Name};{unit.Rarity};{unit.UnitDataId};{unit.ImageUrl}");
        }
    }
}
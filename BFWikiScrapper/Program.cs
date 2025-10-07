using System.Diagnostics;
using System.Text;
using Spectre.Console;

namespace BFWikiScrapper;

public static class Program
{
    public static async Task Main()
    {
        Console.OutputEncoding = Encoding.UTF8;

        AnsiConsole.Clear();
        AnsiConsole.MarkupLine("[bold cyan]Brave Frontier Wiki Scraper[/]\n");

        using var scraper = new WikiScraper(new AnsiScraperLogger());
        var progress = new ScrapeProgress();

        var cts = new CancellationTokenSource();
        var renderer = new LiveProgressRenderer(progress);

        // Start live dashboard
        var renderTask = renderer.StartAsync(cts.Token);

        var stopwatch = Stopwatch.StartNew();
        var units = await scraper.ScrapeUnitsAsync(maxConcurrency: 16, progress: progress, cancellationToken: cts.Token);
        stopwatch.Stop();

        // Stop dashboard
        await cts.CancelAsync();
        await renderTask;

        // Final summary
        AnsiConsole.Clear();
        AnsiConsole.MarkupLine("[bold green]✅ Scraping complete![/]");
        AnsiConsole.Write(new Table()
                          .AddColumn("Metric")
                          .AddColumn("Value")
                          .AddRow("Total Units", units.Count.ToString())
                          .AddRow("Pages Discovered", progress.PagesDiscovered.ToString())
                          .AddRow("Failed Units", progress.FailedUnits.ToString())
                          .AddRow("Duration", $"{stopwatch.Elapsed.TotalSeconds:F1}s")
                          .AddRow("Output", "brave_frontier_units.csv")
                          .Border(TableBorder.Rounded)
        );

        const string outputPath = "brave_frontier_units.csv";
        await WriteCsvAsync(outputPath, units);
    }

    private static async Task WriteCsvAsync(string path, List<UnitData> units)
    {
        await using var writer = new StreamWriter(path);
        await writer.WriteLineAsync("UnitID;Name;Rarity;UnitDataID;ImageUrl");
        foreach ( var unit in units )
            await writer.WriteLineAsync($"{unit.UnitId};{unit.Name};{unit.Rarity};{unit.UnitDataId};{unit.ImageUrl}");
    }
}
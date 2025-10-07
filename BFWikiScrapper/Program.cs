using System.Diagnostics;
using System.Text;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace BFWikiScrapper;

public static class Program
{
    public static async Task Main()
    {
        Console.OutputEncoding = Encoding.UTF8;
        AnsiConsole.Clear();
        AnsiConsole.MarkupLine("[bold cyan underline]Brave Frontier Wiki Scraper[/]\n");

        var maxConcurrency = AnsiConsole.Prompt(
            new TextPrompt<int>("Set max concurrency (threads):")
                .DefaultValue(16)
                .Validate(value => value is > 0 and <= 32 ? ValidationResult.Success() : ValidationResult.Error("Must be between 1-32")));

        using var scraper = new WikiScraper(new AnsiScraperLogger());
        var stopwatch = Stopwatch.StartNew();

        var (units, pagesDiscovered, failedUnits) = await AnsiConsole.Progress()
                                                                     .AutoClear(false)
                                                                     .Columns(
                                                                         new TaskDescriptionColumn(),
                                                                         new ProgressBarColumn(),
                                                                         new CompletedColumn(),
                                                                         new PercentageColumn(),
                                                                         new RemainingTimeColumn(),
                                                                         new SpinnerColumn(Spinner.Known.Arc)
                                                                     )
                                                                     .StartAsync(async ctx => await scraper.ScrapeUnitsAsync(maxConcurrency: maxConcurrency, progressCtx: ctx));

        stopwatch.Stop();

        AnsiConsole.MarkupLine("[bold green]✅   Scraping complete![/]");
        AnsiConsole.Write(new Panel(
            new Table()
                .AddColumn("Metric")
                .AddColumn("Value")
                .AddRow("Total Units", units.Count.ToString())
                .AddRow("Pages Discovered", pagesDiscovered.ToString())
                .AddRow("Failed Units", failedUnits.ToString())
                .AddRow("Duration", $"{stopwatch.Elapsed.TotalSeconds:F1}s")
                .AddRow("Output", "brave_frontier_units.csv")
                .Border(TableBorder.Rounded))
            .Header("[bold cyan]Summary[/]")
            .Border(BoxBorder.Heavy));

        const string outputPath = "brave_frontier_units.csv";
        await WriteCsvAsync(outputPath, units);
    }

    private static async Task WriteCsvAsync(string path, List<UnitData> units)
    {
        await using var writer = new StreamWriter(path);
        await writer.WriteLineAsync("UnitID;Name;Rarity;UnitDataID;ImageUrl");
        foreach (var unit in units)
            await writer.WriteLineAsync($"{unit.UnitId};{unit.Name};{unit.Rarity};{unit.UnitDataId};{unit.ImageUrl}");
    }

    private sealed class CompletedColumn : ProgressColumn
    {
        public override IRenderable Render(RenderOptions options, ProgressTask task, TimeSpan deltaTime)
        {
            if (task.IsIndeterminate || task.MaxValue == 0)
            {
                return new Text(task.Value.ToString("N0"), new Style(Color.Grey));
            }
            return new Text($"{task.Value:N0}/{task.MaxValue:N0}", new Style(Color.White));
        }
    }
}
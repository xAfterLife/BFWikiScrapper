using System.Diagnostics;
using System.Text;
using BFWikiScrapper.Enum;
using BFWikiScrapper.Scrapper;
using BFWikiScrapper.Writer;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace BFWikiScrapper;

public static class Program
{
    public static async Task Main()
    {
        Console.OutputEncoding = Encoding.UTF8;

        while ( true )
        {
            AnsiConsole.Clear();
            AnsiConsole.MarkupLine("[bold cyan underline]Brave Frontier Wiki Scraper[/]\n");

            var mode = AnsiConsole.Prompt(
                new SelectionPrompt<ScraperMode>()
                    .Title("[bold cyan]Select scraping mode:[/]")
                    .AddChoices(ScraperMode.Units, ScraperMode.Levels, ScraperMode.Exit)
            );

            switch ( mode )
            {
                case ScraperMode.Units:
                    await ScrapeUnitsAsync();
                    break;
                case ScraperMode.Levels:
                    await ScrapeLevelsAsync();
                    break;
                case ScraperMode.Exit:
                    AnsiConsole.MarkupLine("[bold yellow]Goodbye![/]");
                    return;
            }

            AnsiConsole.WriteLine();
            AnsiConsole.Markup("[grey]Press any key to return to menu...[/]");
            Console.ReadKey(true);
        }
    }

    private static async Task ScrapeUnitsAsync()
    {
        AnsiConsole.Clear();
        AnsiConsole.MarkupLine("[bold cyan underline]Unit Scraper[/]\n");

        var maxConcurrency = PromptConcurrency();
        var fileWriter = FileFormatHelper.SelectFormat();
        var outputPath = FileFormatHelper.GenerateOutputPath(fileWriter switch
            {
                CsvFileWriter => FileFormat.Csv,
                JsonFileWriter => FileFormat.Json,
                MemoryPackFileWriter => FileFormat.MemoryPack,
                _ => FileFormat.Csv
            }
        );

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
                                                                     .StartAsync(async ctx => await scraper.ScrapeUnitsAsync(
                                                                             maxConcurrency: maxConcurrency,
                                                                             progressCtx: ctx
                                                                         )
                                                                     );

        stopwatch.Stop();

        DisplaySummary("Unit Scraping", [
                ("Total Units", units.Count.ToString()),
                ("Pages Discovered", pagesDiscovered.ToString()),
                ("Failed Units", failedUnits.ToString()),
                ("Duration", $"{stopwatch.Elapsed.TotalSeconds:F1}s"),
                ("Output Format", fileWriter.GetType().Name.Replace("FileWriter", "")),
                ("Output", outputPath)
            ]
        );

        await fileWriter.WriteAsync(outputPath, units);
        AnsiConsole.MarkupLine($"[green]✅ Data written to {outputPath}[/]");
    }

    private static async Task ScrapeLevelsAsync()
    {
        AnsiConsole.Clear();
        AnsiConsole.MarkupLine("[bold cyan underline]Level Data Scraper[/]\n");

        var maxConcurrency = PromptConcurrency();
        var fileWriter = LevelFileFormatHelper.SelectFormat();
        var outputPath = LevelFileFormatHelper.GenerateOutputPath(fileWriter switch
            {
                LevelCsvFileWriter => FileFormat.Csv,
                LevelJsonFileWriter => FileFormat.Json,
                LevelMemoryPackFileWriter => FileFormat.MemoryPack,
                _ => FileFormat.Csv
            }
        );

        using var scraper = new LevelDataScraper(new AnsiScraperLogger());
        var stopwatch = Stopwatch.StartNew();

        var (levels, pagesDiscovered, failedPages) = await AnsiConsole.Progress()
                                                                      .AutoClear(false)
                                                                      .Columns(
                                                                          new TaskDescriptionColumn(),
                                                                          new ProgressBarColumn(),
                                                                          new CompletedColumn(),
                                                                          new PercentageColumn(),
                                                                          new RemainingTimeColumn(),
                                                                          new SpinnerColumn(Spinner.Known.Arc)
                                                                      )
                                                                      .StartAsync(async ctx => await scraper.ScrapeLevelsAsync(
                                                                              maxConcurrency: maxConcurrency,
                                                                              progressCtx: ctx
                                                                          )
                                                                      );

        stopwatch.Stop();

        DisplaySummary("Level Data Scraping", [
                ("Total Levels", levels.Count.ToString()),
                ("Pages Discovered", pagesDiscovered.ToString()),
                ("Failed Pages", failedPages.ToString()),
                ("Duration", $"{stopwatch.Elapsed.TotalSeconds:F1}s"),
                ("Output Format", fileWriter.GetType().Name.Replace("FileWriter", "")),
                ("Output", outputPath)
            ]
        );

        await fileWriter.WriteAsync(outputPath, levels);
        AnsiConsole.MarkupLine($"[green]✅ Data written to {outputPath}[/]");
    }

    private static int PromptConcurrency()
    {
        return AnsiConsole.Prompt(
            new TextPrompt<int>("Set max concurrency (threads):")
                .DefaultValue(16)
                .Validate(value => value is > 0 and <= 32
                    ? ValidationResult.Success()
                    : ValidationResult.Error("Must be between 1-32")
                )
        );
    }

    private static void DisplaySummary(string title, (string Metric, string Value)[] rows)
    {
        AnsiConsole.MarkupLine("[bold green]✅ Scraping complete![/]");

        var table = new Table()
                    .AddColumn("Metric")
                    .AddColumn("Value")
                    .Border(TableBorder.Rounded);

        foreach ( var (metric, value) in rows )
        {
            table.AddRow(metric, value);
        }

        AnsiConsole.Write(new Panel(table)
                          .Header($"[bold cyan]{title} Summary[/]")
                          .Border(BoxBorder.Heavy)
        );
    }

    private enum ScraperMode
    {
        Units = 0,
        Levels = 1,
        Exit = 2
    }

    private sealed class CompletedColumn : ProgressColumn
    {
        public override IRenderable Render(RenderOptions options, ProgressTask task, TimeSpan deltaTime)
        {
            if ( task.IsIndeterminate || task.MaxValue == 0 )
            {
                return new Text(task.Value.ToString("N0"), new Style(Color.Grey));
            }

            return new Text($"{task.Value:N0}/{task.MaxValue:N0}", new Style(Color.White));
        }
    }
}
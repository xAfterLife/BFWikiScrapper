using System.Diagnostics;
using System.Text;
using BFWikiScrapper.Enum;
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
                .Validate(value => value is > 0 and <= 32
                    ? ValidationResult.Success()
                    : ValidationResult.Error("Must be between 1-32")
                )
        );

        // Select output format
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

        AnsiConsole.MarkupLine("[bold green]✅ Scraping complete![/]");
        AnsiConsole.Write(new Panel(
                              new Table()
                                  .AddColumn("Metric")
                                  .AddColumn("Value")
                                  .AddRow("Total Units", units.Count.ToString())
                                  .AddRow("Pages Discovered", pagesDiscovered.ToString())
                                  .AddRow("Failed Units", failedUnits.ToString())
                                  .AddRow("Duration", $"{stopwatch.Elapsed.TotalSeconds:F1}s")
                                  .AddRow("Output Format", fileWriter.GetType().Name.Replace("FileWriter", ""))
                                  .AddRow("Output", outputPath)
                                  .Border(TableBorder.Rounded)
                          )
                          .Header("[bold cyan]Summary[/]")
                          .Border(BoxBorder.Heavy)
        );

        // Serialize to chosen format
        await fileWriter.WriteAsync(outputPath, units);
        AnsiConsole.MarkupLine($"[green]✅ Data written to {outputPath}[/]");
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
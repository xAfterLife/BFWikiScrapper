using Spectre.Console;

namespace BFWikiScrapper;

public sealed class LiveProgressRenderer
{
    private readonly ScrapeProgress _progress;

    public LiveProgressRenderer(ScrapeProgress progress)
    {
        _progress = progress;
    }

    public async Task StartAsync(CancellationToken token)
    {
        try
        {
            await AnsiConsole.Live(BuildTable())
                             .AutoClear(false)
                             .StartAsync(async ctx =>
                                 {
                                     while ( !token.IsCancellationRequested )
                                     {
                                         ctx.UpdateTarget(BuildTable());
                                         await Task.Delay(250, token);
                                     }
                                 }
                             );
        }
        catch ( OperationCanceledException )
        {
            // Normal exit — happens when the token is cancelled
        }
    }

    private Table BuildTable()
    {
        var table = new Table()
                    .Border(TableBorder.Rounded)
                    .Title("[bold cyan]Scraping Progress[/]")
                    .AddColumn("Metric")
                    .AddColumn("Value");

        table.AddRow("Pages Discovered", _progress.PagesDiscovered.ToString());
        table.AddRow("Units Scraped", _progress.UnitsScraped.ToString());
        table.AddRow("Failed Units", _progress.FailedUnits.ToString());
        table.AddRow("Active Tasks", _progress.ActiveTasks.ToString());
        table.AddRow("Last Updated", DateTime.Now.ToLongTimeString());

        return table;
    }
}
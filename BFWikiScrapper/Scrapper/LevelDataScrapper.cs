using AngleSharp;
using BFWikiScrapper.Model;
using Spectre.Console;

namespace BFWikiScrapper.Scrapper;

public sealed class LevelDataScraper : IDisposable
{
    private readonly IBrowsingContext _browsingContext;
    private readonly HttpClient _httpClient;
    private readonly AnsiScraperLogger _logger;
    private readonly string _wikiBaseUrl;

    public LevelDataScraper(AnsiScraperLogger logger, string wikiBaseUrl = "https://bravefrontierglobal.fandom.com")
    {
        _logger = logger;
        _wikiBaseUrl = wikiBaseUrl;
        _httpClient = new HttpClient(new SocketsHttpHandler
            {
                PooledConnectionLifetime = TimeSpan.FromMinutes(5),
                MaxConnectionsPerServer = 16
            }
        )
        {
            Timeout = TimeSpan.FromSeconds(30)
        };

        var config = Configuration.Default.WithDefaultLoader();
        _browsingContext = BrowsingContext.New(config);
    }

    public void Dispose()
    {
        _httpClient.Dispose();
        _browsingContext.Dispose();
    }

    public async Task<(List<LevelData> Levels, int PagesDiscovered, int FailedPages)> ScrapeLevelsAsync(string initialPagePath = "/wiki/Player_Level#Level_1_-_100",
        int maxConcurrency = 8,
        ProgressContext? progressCtx = null,
        CancellationToken cancellationToken = default)
    {
        var discoverTask = progressCtx?.AddTask("[green]Discovering Level Pages[/]");
        var pageUrls = new List<string>
        {
            "https://bravefrontierglobal.fandom.com/wiki/Player_Level#Level_1_-_100",
            "https://bravefrontierglobal.fandom.com/wiki/Player_Level#Level_101_-_200",
            "https://bravefrontierglobal.fandom.com/wiki/Player_Level#Level_201_-_300",
            "https://bravefrontierglobal.fandom.com/wiki/Player_Level#Level_301_-_400",
            "https://bravefrontierglobal.fandom.com/wiki/Player_Level#Level_401_-_500",
            "https://bravefrontierglobal.fandom.com/wiki/Player_Level#Level_501_-_600",
            "https://bravefrontierglobal.fandom.com/wiki/Player_Level#Level_601_-_700",
            "https://bravefrontierglobal.fandom.com/wiki/Player_Level#Level_701_-_800",
            "https://bravefrontierglobal.fandom.com/wiki/Player_Level#Level_801_-_900",
            "https://bravefrontierglobal.fandom.com/wiki/Player_Level#Level_901_-_999"
        };
        var pagesDiscovered = pageUrls.Count;

        _logger.Info($"Total pages discovered: {pagesDiscovered}");

        discoverTask?.Value(100);
        discoverTask?.StopTask();

        var failedPages = 0;
        var failedTask = progressCtx?.AddTask("[red]Failed Pages[/]", maxValue: pagesDiscovered);
        var scrapeTask = progressCtx?.AddTask("[green]Scraped Pages[/]", maxValue: pagesDiscovered);

        var results = new List<LevelData>(1000);
        var semaphore = new SemaphoreSlim(maxConcurrency);
        var lockObj = new object();

        var tasks = pageUrls.Select(async url =>
            {
                await semaphore.WaitAsync(cancellationToken);
                try
                {
                    var levels = await ScrapeLevelPageAsync(url, cancellationToken);
                    if ( levels.Count > 0 )
                    {
                        lock ( lockObj )
                        {
                            results.AddRange(levels);
                        }

                        scrapeTask?.Increment(1);
                        return levels.Count;
                    }
                    else
                    {
                        failedTask?.Increment(1);
                        Interlocked.Increment(ref failedPages);
                        return 0;
                    }
                }
                finally
                {
                    semaphore.Release();
                }
            }
        );

        await Task.WhenAll(tasks);
        scrapeTask?.StopTask();
        failedTask?.StopTask();

        // Dedupe and sort
        var uniqueLevels = results
                           .DistinctBy(l => l.Level)
                           .OrderBy(l => l.Level)
                           .ToList();

        return (uniqueLevels, pagesDiscovered, failedPages);
    }

    private async Task<List<LevelData>> ScrapeLevelPageAsync(string url, CancellationToken cancellationToken)
    {
        try
        {
            var html = await _httpClient.GetStringAsync(url, cancellationToken);
            var document = await _browsingContext.OpenAsync(req => req.Content(html), cancellationToken);

            var levels = new List<LevelData>(100);

            // Fix: Use correct CSS selector with dots for classes
            // Try multiple selectors to be more robust
            var tables = document.QuerySelectorAll("table.article-table, table.wikitable, div.wds-tab__content table");

            _logger.Info($"Found {tables.Length} table(s) on {url}");

            foreach ( var table in tables )
            {
                var rows = table.QuerySelectorAll("tr");

                foreach ( var row in rows )
                {
                    var cells = row.QuerySelectorAll("td").ToArray();
                    if ( cells.Length == 0 )
                        continue;

                    var firstCell = cells[0];
                    var style = firstCell.GetAttribute("style");

                    // Check if this is a level row (bold first cell)
                    if ( string.IsNullOrEmpty(style) ||
                         !style.Contains("font-weight:bold") && !style.Contains("font-weight: bold") )
                        continue;

                    var levelText = firstCell.TextContent.Trim();
                    if ( !int.TryParse(levelText, out var level) )
                    {
                        _logger.Warn($"Failed to parse level: '{levelText}'");
                        continue;
                    }

                    // Get the last cell which should contain XP
                    var lastCell = cells[^1];
                    var xpText = lastCell.TextContent.Trim()
                                         .Replace(",", "")
                                         .Replace(".", "")
                                         .Replace(" ", "");

                    if ( !long.TryParse(xpText, out var xp) )
                    {
                        _logger.Warn($"Failed to parse XP: '{xpText}' for level {level}");
                        continue;
                    }

                    levels.Add(new LevelData(level, xp));
                }
            }

            _logger.Info($"Extracted {levels.Count} levels from {url}");
            return levels;
        }
        catch ( HttpRequestException ex )
        {
            _logger.Warn($"Failed to fetch {url}: {ex.Message}");
            return [];
        }
        catch ( Exception ex )
        {
            _logger.Error($"Error scraping {url}: {ex.Message}", ex);
            return [];
        }
    }
}
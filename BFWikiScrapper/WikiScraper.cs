using AngleSharp;
using AngleSharp.Dom;
using Spectre.Console;

namespace BFWikiScrapper;

public sealed class WikiScraper : IDisposable
{
    private readonly IBrowsingContext _browsingContext;
    private readonly HttpClient _httpClient;
    private readonly AnsiScraperLogger _logger;
    private readonly string _wikiBaseUrl;

    public WikiScraper(AnsiScraperLogger logger, string wikiBaseUrl = "https://bravefrontierglobal.fandom.com")
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

    public async Task<(List<UnitData> Units, int PagesDiscovered, int FailedUnits)> ScrapeUnitsAsync(string unitListUrl = "/wiki/Unit_List", int maxConcurrency = 8, ProgressContext? progressCtx = null, CancellationToken cancellationToken = default)
    {
        var discoverTask = progressCtx?.AddTask("[green]Discovered List-Pages[/]");
        var listPageUrls = await DiscoverAllListPagesAsync(unitListUrl, cancellationToken);
        var pagesDiscovered = listPageUrls.Count;
        discoverTask?.Value(100);
        discoverTask?.StopTask();

        var extractTask = progressCtx?.AddTask("[green]Extracted Unit URLs[/]", maxValue: pagesDiscovered);
        var unitUrls = await ScrapeAllListPagesAsync(listPageUrls, maxConcurrency, extractTask, cancellationToken);

        var failedUnits = 0;
        var failedTask = progressCtx?.AddTask("[red]Failed Pages[/]", maxValue: unitUrls.Count);
        var scrapeTask = progressCtx?.AddTask("[green]Scrapped Units[/]", maxValue: unitUrls.Count);

        var results = new List<UnitData>(unitUrls.Count);
        var semaphore = new SemaphoreSlim(maxConcurrency);
        var tasks = unitUrls.Select(async url =>
            {
                await semaphore.WaitAsync(cancellationToken);
                try
                {
                    var data = await ScrapeUnitPageAsync(url, cancellationToken);
                    if ( data is not null )
                    {
                        scrapeTask?.Increment(1);
                        return data;
                    }
                    else
                    {
                        failedTask?.Increment(1);
                        Interlocked.Increment(ref failedUnits);
                        return null;
                    }
                }
                finally
                {
                    semaphore.Release();
                }
            }
        );

        results.AddRange((await Task.WhenAll(tasks)).OfType<UnitData>());

        scrapeTask?.StopTask();
        failedTask?.StopTask();

        return (results, pagesDiscovered, failedUnits);
    }

    private async Task<List<string>> DiscoverAllListPagesAsync(string initialListPagePath, CancellationToken cancellationToken)
    {
        var fullUrl = _wikiBaseUrl + initialListPagePath;
        var html = await _httpClient.GetStringAsync(fullUrl, cancellationToken);
        var document = await _browsingContext.OpenAsync(req => req.Content(html), cancellationToken);
        var listPages = new List<string> { fullUrl };
        var paginationLinks = document.QuerySelectorAll("a[href*='/wiki/Unit_List:']")
                                      .Select(a => a.GetAttribute("href"))
                                      .Where(href => !string.IsNullOrEmpty(href))
                                      .Select(href => href!.StartsWith("http") ? href : _wikiBaseUrl + href)
                                      .Distinct()
                                      .ToList();
        listPages.AddRange(paginationLinks);
        _logger.Info($"Discovered {paginationLinks.Count} paginated list pages");
        return listPages;
    }

    private async Task<HashSet<string>> ScrapeAllListPagesAsync(List<string> listPageUrls, int maxConcurrency, ProgressTask? extractTask, CancellationToken cancellationToken)
    {
        var allUnitUrls = new HashSet<string>(5000);
        var semaphore = new SemaphoreSlim(maxConcurrency);
        var lockObj = new object();
        var tasks = listPageUrls.Select(async listPageUrl =>
            {
                await semaphore.WaitAsync(cancellationToken);
                try
                {
                    var urls = await ExtractUnitUrlsFromListPageAsync(listPageUrl, cancellationToken);
                    lock ( lockObj )
                    {
                        foreach ( var url in urls )
                        {
                            allUnitUrls.Add(url);
                        }
                    }

                    _logger.Info($"Extracted {urls.Count} units from {listPageUrl}");
                    return urls.Count;
                }
                finally
                {
                    extractTask?.Increment(1);
                    semaphore.Release();
                }
            }
        );
        await Task.WhenAll(tasks);
        extractTask?.StopTask();
        return allUnitUrls;
    }

    private async Task<List<string>> ExtractUnitUrlsFromListPageAsync(string listPageUrl, CancellationToken cancellationToken)
    {
        var html = await _httpClient.GetStringAsync(listPageUrl, cancellationToken);
        var document = await _browsingContext.OpenAsync(req => req.Content(html), cancellationToken);
        var urls = new List<string>(1000);
        var links = document.QuerySelectorAll("table.wikitable a[href*='/wiki/']")
                            .Select(a => a.GetAttribute("href"))
                            .Where(href => !string.IsNullOrEmpty(href) &&
                                           !href.Contains("File:") &&
                                           !href.Contains("Category:") &&
                                           !href.Contains("Special:") &&
                                           !href.Contains("Template:") &&
                                           !href.Contains("Unit_List")
                            )
                            .Distinct();
        foreach ( var link in links )
        {
            var fullLink = link != null && link.StartsWith("http") ? link : _wikiBaseUrl + link;
            urls.Add(fullLink);
        }

        return urls;
    }

    private async Task<UnitData?> ScrapeUnitPageAsync(string url, CancellationToken cancellationToken)
    {
        try
        {
            var html = await _httpClient.GetStringAsync(url, cancellationToken);
            var document = await _browsingContext.OpenAsync(req => req.Content(html), cancellationToken);
            var name = document.QuerySelector("div.unit-header b")?.TextContent.Trim() ?? string.Empty;
            var (unitId, unitDataId, rarity) = ExtractUnitIdsAndRarity(document, url);
            if ( string.IsNullOrEmpty(unitId) || string.IsNullOrEmpty(unitDataId) )
            {
                _logger.Warn($"Missing IDs for {url}");
                return null;
            }

            var imageUrl = await ExtractSplashArtUrlAsync(document);
            if ( string.IsNullOrEmpty(imageUrl) )
                return null;
            return new UnitData(unitId, name, rarity, unitDataId, imageUrl);
        }
        catch ( HttpRequestException ex )
        {
            _logger.Warn($"Failed to fetch {url}: {ex.Message}");
            return null;
        }
        catch ( Exception ex )
        {
            _logger.Error($"Error scraping {url}: {ex.Message}");
            return null;
        }
    }

    private (string unitId, string unitDataId, string rarity) ExtractUnitIdsAndRarity(IDocument document, string url)
    {
        var unitInfoBox = document.QuerySelector("div.unit-info.unit-box");
        if ( unitInfoBox != null )
        {
            var rows = unitInfoBox.QuerySelectorAll("tr");
            string unitId = string.Empty, unitDataId = string.Empty, rarity = string.Empty;
            foreach ( var row in rows )
            {
                var header = row.QuerySelector("th")?.TextContent.Trim();
                var value = row.QuerySelector("td")?.TextContent.Trim();
                if ( string.IsNullOrEmpty(header) || string.IsNullOrEmpty(value) )
                    continue;
                switch ( header )
                {
                    case "Unit No.":
                        unitId = value;
                        break;
                    case "Data ID":
                        unitDataId = value;
                        break;
                    case "Rarity":
                        rarity = value;
                        break;
                }

                if ( !string.IsNullOrEmpty(unitId) && !string.IsNullOrEmpty(unitDataId) && !string.IsNullOrEmpty(rarity) )
                    return (unitId, unitDataId, rarity);
            }
        }

        _logger.Warn($"Failed to extract unit IDs or rarity from {url}");
        return (string.Empty, string.Empty, string.Empty);
    }

    private ValueTask<string> ExtractSplashArtUrlAsync(IDocument document)
    {
        var imageLinks = document.QuerySelectorAll("a.mw-file-description.image");
        if ( imageLinks.Length < 2 )
        {
            if ( imageLinks.Length > 0 )
            {
                _logger.Warn("Failed to find image link");
                return ValueTask.FromResult(string.Empty);
            }

            _logger.Info("Using Fallback element");
            var href = imageLinks[0].GetAttribute("href");
            return ValueTask.FromResult(string.IsNullOrEmpty(href) ? string.Empty : NormalizeUrl(href));
        }

        var secondLink = imageLinks[1];
        var secondHref = secondLink.GetAttribute("href");
        return ValueTask.FromResult(string.IsNullOrEmpty(secondHref) ? string.Empty : NormalizeUrl(secondHref));
    }

    private static string NormalizeUrl(string url)
    {
        var cleanUrl = url.Split(["/scale-to-width-down/"], StringSplitOptions.None)[0];
        if ( cleanUrl.StartsWith("//") )
        {
            return "https:" + cleanUrl;
        }

        if ( !cleanUrl.StartsWith("http") )
        {
            return "https://bravefrontierglobal.fandom.com" + cleanUrl;
        }

        return cleanUrl;
    }
}
using Spectre.Console;

namespace BFWikiScrapper;

public sealed class AnsiScraperLogger
{
    private readonly Lock _sync = new();

    public void Info(string message)
    {
        lock ( _sync )
        {
            AnsiConsole.MarkupLineInterpolated($"[grey]{message.EscapeMarkup()}[/]");
        }
    }

    public void Warn(string message)
    {
        lock ( _sync )
        {
            AnsiConsole.MarkupLineInterpolated($"[yellow]{message.EscapeMarkup()}[/]");
        }
    }

    public void Error(string message, Exception? ex = null)
    {
        lock ( _sync )
        {
            AnsiConsole.MarkupLineInterpolated($"[red]{message.EscapeMarkup()}[/] {ex?.Message}");
        }
    }
}
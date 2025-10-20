using BFWikiScrapper.Enum;
using BFWikiScrapper.Model;
using BFWikiScrapper.Writer;
using Spectre.Console;

namespace BFWikiScrapper;

public static class LevelFileFormatHelper
{
    public static IFileWriter<LevelData> SelectFormat()
    {
        var format = AnsiConsole.Prompt(
            new SelectionPrompt<FileFormat>()
                .Title("[bold cyan]Select output format:[/]")
                .AddChoices(FileFormat.Csv, FileFormat.Json, FileFormat.MemoryPack)
        );

        return CreateWriter(format);
    }

    public static IFileWriter<LevelData> CreateWriter(FileFormat format)
    {
        return format switch
        {
            FileFormat.Csv => new LevelCsvFileWriter(),
            FileFormat.Json => new LevelJsonFileWriter(),
            FileFormat.MemoryPack => new LevelMemoryPackFileWriter(),
            _ => throw new ArgumentException($"Unknown format: {format}", nameof(format))
        };
    }

    public static string GenerateOutputPath(FileFormat format)
    {
        var writer = CreateWriter(format);
        return $"brave_frontier_levels{writer.FileExtension}";
    }
}
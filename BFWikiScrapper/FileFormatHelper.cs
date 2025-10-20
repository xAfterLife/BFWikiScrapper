using BFWikiScrapper.Enum;
using BFWikiScrapper.Interface;
using BFWikiScrapper.Writer;
using Spectre.Console;

namespace BFWikiScrapper;

/// <summary>
///     Factory and helper for file format selection and writer instantiation.
/// </summary>
public static class FileFormatHelper
{
    /// <summary>
    ///     Prompt the user to select an output format and return the appropriate writer.
    /// </summary>
    public static IFileWriter SelectFormat()
    {
        var format = AnsiConsole.Prompt(
            new SelectionPrompt<FileFormat>()
                .Title("[bold cyan]Select output format:[/]")
                .AddChoices(FileFormat.Csv, FileFormat.Json, FileFormat.MemoryPack)
        );

        return CreateWriter(format);
    }

    /// <summary>
    ///     Create a writer instance for the specified format.
    /// </summary>
    public static IFileWriter CreateWriter(FileFormat format)
    {
        return format switch
        {
            FileFormat.Csv => new CsvFileWriter(),
            FileFormat.Json => new JsonFileWriter(),
            FileFormat.MemoryPack => new MemoryPackFileWriter(),
            _ => throw new ArgumentException($"Unknown format: {format}", nameof(format))
        };
    }

    /// <summary>
    ///     Generate an output file path with the appropriate extension.
    /// </summary>
    public static string GenerateOutputPath(FileFormat format)
    {
        var writer = CreateWriter(format);
        return $"brave_frontier_units{writer.FileExtension}";
    }
}
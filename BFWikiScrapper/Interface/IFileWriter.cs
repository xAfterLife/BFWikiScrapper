using BFWikiScrapper.Model;

namespace BFWikiScrapper.Interface;

/// <summary>
///     Abstraction for format-specific file writing.
/// </summary>
public interface IFileWriter
{
    /// <summary>
    ///     Return the recommended file extension for this format.
    /// </summary>
    string FileExtension { get; }

    /// <summary>
    ///     Write units to the specified file path asynchronously.
    /// </summary>
    ValueTask WriteAsync(string filePath, List<UnitData> units, CancellationToken cancellationToken = default);
}
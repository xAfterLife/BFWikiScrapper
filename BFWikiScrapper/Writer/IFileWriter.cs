namespace BFWikiScrapper.Writer;

public interface IFileWriter<T>
{
    string FileExtension { get; }
    ValueTask WriteAsync(string filePath, List<T> data, CancellationToken cancellationToken = default);
}
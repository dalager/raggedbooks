using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;
using UglyToad.PdfPig.Outline;

namespace RaggedBooks.Core;

public static class TextExtractor
{
    public record Page(string TextContent, int pagenumber);

    public record Book(
        string Title,
        List<Page> Pages,
        BookmarkTree BookmarkTree,
        string Authors,
        string Filename
    );

    public static async Task<Book> LoadBook(string fileName)
    {
        if (!File.Exists(fileName))
        {
            throw new FileNotFoundException("File not found", fileName);
        }

        await using var stream = File.OpenRead(fileName);

        var pages = await GetContentAsync(stream);
        _ = stream.Seek(0, SeekOrigin.Begin);
        var chapters = await GetChapters(stream);

        var bookmarkTree = new BookmarkTree(chapters);
        _ = stream.Seek(0, SeekOrigin.Begin);
        using var pdfDocument = PdfDocument.Open(stream);
        var title = pdfDocument.Information.Title ?? string.Empty;
        var authors = pdfDocument.Information.Author ?? string.Empty;

        return new Book(title, pages, bookmarkTree, authors, Path.GetFileName(fileName));
    }

    public static Task<List<Page>> GetContentAsync(Stream stream)
    {
        var pages = new List<Page>();

        // Read the content of the PDF document.
        using var pdfDocument = PdfDocument.Open(stream);

        // get the title of the book

        foreach (var page in pdfDocument.GetPages().Where(x => x is not null))
        {
            var pageContent = ContentOrderTextExtractor.GetText(page) ?? string.Empty;
            pages.Add(new Page(pageContent, page.Number));
        }

        return Task.FromResult(pages);
    }

    public record Chapter(string Title, int Level, int Pagenumber);

    public static Task<List<Chapter>> GetChapters(Stream stream)
    {
        var result = new List<Chapter>();
        // Read the content of the PDF document.
        using var pdfDocument = PdfDocument.Open(stream);
        if (pdfDocument.TryGetBookmarks(out var bookmarks))
        {
            var bookmarkNodes = bookmarks.GetNodes();
            foreach (BookmarkNode node in bookmarkNodes)
            {
                if (node is DocumentBookmarkNode docmark)
                {
                    result.Add(new Chapter(docmark.Title, docmark.Level, docmark.PageNumber));
                }
            }
        }

        return Task.FromResult(result);
    }
}

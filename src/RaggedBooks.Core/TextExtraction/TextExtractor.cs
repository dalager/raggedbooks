using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;
using UglyToad.PdfPig.Outline;

namespace RaggedBooks.Core.TextExtraction;

public interface IConvertToBook
{
    Task<Book> Convert(string fileName);
}

public class PdfToBookConverter : IConvertToBook
{
    public async Task<Book> Convert(string fileName)
    {
        using var pdfDocument = PdfDocument.Open(fileName);

        var pages = await GetContentAsync(pdfDocument);

        var chapters = await GetChapters(pdfDocument);

        var chapterPath = new ChapterPath(chapters);

        var title = pdfDocument.Information.Title ?? string.Empty;
        var authors = pdfDocument.Information.Author ?? string.Empty;

        return new Book(title, pages, chapterPath, authors, Path.GetFileName(fileName));
    }

    private Task<List<Page>> GetContentAsync(PdfDocument pdfDocument)
    {
        var pages = new List<Page>();

        // get the title of the book
        foreach (var page in pdfDocument.GetPages().Where(x => x is not null))
        {
            var pageContent = ContentOrderTextExtractor.GetText(page) ?? string.Empty;
            pages.Add(new Page(pageContent, page.Number));
        }

        return Task.FromResult(pages);
    }

    private Task<List<Chapter>> GetChapters(PdfDocument pdfDocument)
    {
        var result = new List<Chapter>();

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
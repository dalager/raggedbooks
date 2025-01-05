using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;
using UglyToad.PdfPig.Outline;

public static class TextExtractor
{
    public record Page(string TextContent, int pagenumber);

    public static Task<List<Page>> GetContentAsync(Stream stream)
    {
        var pages = new List<Page>();

        // Read the content of the PDF document.
        using var pdfDocument = PdfDocument.Open(stream);

        foreach (var page in pdfDocument.GetPages().Where(x => x is not null))
        {
            var pageContent = ContentOrderTextExtractor.GetText(page) ?? string.Empty;
            pages.Add(new Page(pageContent, page.Number));
        }

        return Task.FromResult(pages);
    }

    public record Chapter(string Title, int Level, int Pagenumber);

    public static List<Chapter> GetChapters(Stream stream)
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

        return result;
    }
}

public class BookmarkTree
{
    private readonly List<TextExtractor.Chapter> _chapters;
    private readonly Dictionary<int, string> _chapterPathCache = new();

    public BookmarkTree(List<TextExtractor.Chapter> chapters)
    {
        _chapters = chapters;
    }

    public string GetChapterPath(int pageNumber, int maxLevel = int.MaxValue)
    {
        if (_chapterPathCache.TryGetValue(pageNumber, out var cachedPath))
        {
            return cachedPath;
        }

        var path = new List<string>();
        var currentLevel = 0;

        void AddTitle(string title, int? index = 0)
        {
            // remove linebreaks
            title ??= string.Empty;
            title = title.ReplaceLineEndings(" ");
            title = title.Replace("  ", " ");
            if (index.HasValue && path.Count > 0)
            {
                path[index.Value] = title;
            }
            else
            {
                path.Add(title);
            }
        }

        foreach (var chapter in _chapters)
        {
            if (chapter.Pagenumber > pageNumber)
                break;

            if (chapter.Level > maxLevel)
                continue;

            if (chapter.Level > currentLevel)
            {
                AddTitle(chapter.Title);
                currentLevel = chapter.Level;
            }
            else if (chapter.Level == currentLevel)
            {
                if (path.Count > 0)
                {
                    AddTitle(chapter.Title, path.Count - 1);
                }
                else
                {
                    AddTitle(chapter.Title);
                }
            }
            else
            {
                while (currentLevel >= chapter.Level && path.Count > 0)
                {
                    path.RemoveAt(path.Count - 1);
                    currentLevel--;
                }
                AddTitle(chapter.Title);
                currentLevel = chapter.Level;
            }
        }

        var resultPath = string.Join(" > ", path);

        _chapterPathCache[pageNumber] = resultPath;

        return resultPath;
    }
}

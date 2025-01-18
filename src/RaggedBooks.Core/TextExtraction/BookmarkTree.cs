namespace RaggedBooks.Core.TextExtraction;

/// <summary>
/// Represents a tree of bookmarks for a PDF document.
/// Used to enrich the chunks with metadata
/// </summary>
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

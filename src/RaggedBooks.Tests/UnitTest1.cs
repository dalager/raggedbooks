using RaggedBooks.Core;

namespace RaggedBooks.Tests;

public class Tests
{
    [Test]
    public async Task Test1()
    {
        var file =
            @"C:\projects\dalager\1 Projects\raggedbooks\data\softwarearchitecture_thehardparts.pdf";
        var stream = File.OpenRead(file);
        var chaps = await TextExtractor.GetChapters(stream);
        Assert.That(chaps, Is.Not.Null);

        var bookmarktree = new BookmarkTree(chaps);
        var pages = await TextExtractor.GetContentAsync(File.OpenRead(file));
        Assert.That(pages, Is.Not.Null);
        foreach (var page in pages)
        {
            Console.WriteLine(
                page.pagenumber + ":" + bookmarktree.GetChapterPath(page.pagenumber, 1)
            );
        }
    }
}

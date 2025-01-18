using RaggedBooks.Core.TextExtraction;

namespace RaggedBooks.Tests;

public class TextExtractorIntegrationTests
{
    [Test]
    public async Task Test1()
    {
        var file =
            @"C:\projects\dalager\1 Projects\raggedbooks\data\softwarearchitecture_thehardparts.pdf";
        var stream = File.OpenRead(file);
        var chaps = await TextExtractor.GetChapters(stream);
        Assert.That(chaps, Is.Not.Null);
        var book = await TextExtractor.LoadBook(file);
        Assert.That(book, Is.Not.Null);
        Assert.That(book.Title, Is.EqualTo("Software Architecture: The Hard Parts"));
        Assert.That(book.Pages.Count, Is.GreaterThanOrEqualTo(199));
    }
}

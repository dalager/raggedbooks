using RaggedBooks.Core.Configuration;
using RaggedBooks.Core.SemanticSearch;
using RaggedBooks.Core.TextExtraction;

namespace RaggedBooks.Tests;

public class TextExtractorIntegrationTests
{
    [Test]
    public async Task Pdf_Extraction_Succeeds()
    {
        //Arrange
        var file = string.Concat(
            Environment.CurrentDirectory,
            Path.DirectorySeparatorChar,
            "data",
            Path.DirectorySeparatorChar,
            "warandpeace.pdf"); //public domain book

        var sut = new PdfToBookConverter();

        //Act
        var book = await sut.Convert(file);

        //Assert
        Assert.That(book, Is.Not.Null);
        Assert.That(book.Title, Is.EqualTo("War and Peace"));
        Assert.That(book.Pages.Count, Is.GreaterThanOrEqualTo(1000));
    }
}

public class VectorSearchServiceTests
{
    [Test]
    public async Task TestGetBooks()
    {
        var svc = new QDrantApiClient(
            new RaggedBookConfig() { QdrantUrl = new Uri("http://localhost:6333") }
        );

        var books = await svc.GetBooks();
        Assert.That(books.Keys.Count, Is.GreaterThanOrEqualTo(1));
    }
}

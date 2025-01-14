using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Embeddings;
using Microsoft.SemanticKernel.Text;

#pragma warning disable SKEXP0050
#pragma warning disable SKEXP0001

namespace RaggedBooks.Core;

public class FileImportService
{
    private readonly ITextEmbeddingGenerationService _textEmbeddingGenerationService;
    private readonly VectorSearchService _vectorSearchService;
    private readonly ILogger<FileImportService> _logger;
    private readonly RaggedBookConfig _config;

    public FileImportService(
        Kernel kernel,
        VectorSearchService vectorSearchService,
        ILogger<FileImportService> logger,
        IOptions<RaggedBookConfig> config
    )
    {
        _textEmbeddingGenerationService =
            kernel.GetRequiredService<ITextEmbeddingGenerationService>();
        _vectorSearchService = vectorSearchService;
        this._logger = logger;
        _config = config.Value;
    }

    public async Task ImportFileAndCreateEmbeddingsInFolder(string[] args)
    {
        var folder = args[1];
        if (!Directory.Exists(folder))
        {
            Console.WriteLine($"Folder {folder} does not exist");
            return;
        }
        var files = Directory.GetFiles(folder, "*.pdf");
        foreach (var file in files)
        {
            await ImportFileAndCreateEmbeddings(new[] { "import-file", file });
        }
    }

    public async Task ImportFileAndCreateEmbeddings(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Please provide a file to import");
            return;
        }
        var file = args[1];
        if (!File.Exists(file))
        {
            Console.WriteLine($"File {file} does not exist");
            return;
        }

        _logger.LogInformation("Importing file: {Bookname}", file);
        var book = await TextExtractor.LoadBook(file);

        var pages = book.Pages;
        var chunks = new List<ContentChunk>();
        var sw = Stopwatch.StartNew();
        _logger.LogInformation(
            "Found {PageCount} pages in {BookTitle} {FileName}. Creating embeddings...",
            pages.Count,
            book.Title,
            book.Filename
        );
        int bookIndex = 0;
        foreach (var page in pages)
        {
            var paragraphs = TextChunker.SplitPlainTextParagraphs(
                TextChunker.SplitPlainTextLines(page.TextContent, _config.MaxTokensPerLine),
                _config.MaxTokensPerParagraph,
                _config.OverlapTokens
            );

            var embeddings = await _textEmbeddingGenerationService.GenerateEmbeddingsAsync(
                paragraphs
            );

            foreach (
                var (index, paragraph) in Enumerable.Select<string, (int index, string x)>(
                    paragraphs,
                    (x, index) => (index, x)
                )
            )
            {
                var embedding = embeddings[index];
                var chunk = new ContentChunk
                {
                    Id = Guid.NewGuid(),
                    Book = book.Title,
                    Chapter = book.BookmarkTree.GetChapterPath(page.pagenumber),
                    PageNumber = page.pagenumber,
                    Content = paragraph,
                    ContentEmbedding = embedding,
                    Index = bookIndex,
                    BookFilename = book.Filename,
                };
                chunks.Add(chunk);
                bookIndex++;
            }
        }
        _logger.LogInformation(
            "Created {ChunkCount} embeddings in {ElapsedMilliseconds}ms, averaging {AvgMs}ms per embedding",
            chunks.Count,
            sw.ElapsedMilliseconds,
            sw.ElapsedMilliseconds / chunks.Count
        );

        await _vectorSearchService.UpsertItems(chunks.ToArray());
    }
}

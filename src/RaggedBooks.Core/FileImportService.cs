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

    public FileImportService(Kernel kernel, VectorSearchService vectorSearchService)
    {
        _textEmbeddingGenerationService =
            kernel.GetRequiredService<ITextEmbeddingGenerationService>();
        _vectorSearchService = vectorSearchService;
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

        var bookname = Path.GetFileNameWithoutExtension(file);

        Console.WriteLine($"Importing file: {bookname}");

        var pages = await TextExtractor.GetContentAsync(File.OpenRead(file));
        const int MaxTokensPerLine = 300;
        const int MaxTokensPerParagraph = 512; // 1024 for ada, 512 for mxbai
        const int OverlapTokens = 100;
        var chapters = TextExtractor.GetChapters(File.OpenRead(file));
        var bookmarktree = new BookmarkTree(chapters);
        var chunks = new List<ContentChunk>();

        foreach (var page in pages)
        {
            var paragraphs = TextChunker.SplitPlainTextParagraphs(
                TextChunker.SplitPlainTextLines(page.TextContent, MaxTokensPerLine),
                MaxTokensPerParagraph,
                OverlapTokens
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
                    Key = Guid.NewGuid(),
                    Book = bookname,
                    Chapter = bookmarktree.GetChapterPath(page.pagenumber),
                    PageNumber = page.pagenumber,
                    Content = paragraph,
                    ContentEmbedding = embedding,
                };
                chunks.Add(chunk);
            }

            await _vectorSearchService.UpsertItems(chunks.ToArray());
        }
    }
}

#pragma warning disable SKEXP0010
#pragma warning disable SKEXP0001
#pragma warning disable SKEXP0050
#pragma warning disable SKEXP0070
using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.Qdrant;
using Microsoft.SemanticKernel.Embeddings;
using Microsoft.SemanticKernel.Text;
using Qdrant.Client;

namespace Embedder;

internal class Program
{
    private static ChatService? _chatService;
    private static ITextEmbeddingGenerationService? _textEmbeddingGenerationService;
    private static readonly AzureOpenAiSettings azureOpenAiSettings = new();
    private static readonly RaggedBookConfig raggedBookConfig = new();

    private static async global::System.Threading.Tasks.Task Main(string[] args)
    {
        // add IConfiguration from Appsettings.json
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        configuration.GetSection("AzureOpenAiSettings").Bind(azureOpenAiSettings);
        azureOpenAiSettings.ValidateConfiguration();
        configuration.GetSection("AppSettings").Bind(raggedBookConfig);
        raggedBookConfig.ValidateConfiguration();

        Kernel kernel = InitializeKernel();
        _chatService = new ChatService(kernel);
        if (args.Length == 0)
        {
            Console.WriteLine(
                "Valid commands: \n"
                    + "import-file <file> - Import a file and create embeddings\n"
                    + "import-folder <folder> - Import all pdf files in folders and create embeddings\n"
                    + "search <query> [-content] [-open] Search for embeddings\n"
            );
            return;
        }

        if (args[0] == "import-file")
            await ImportFileAndCreateEmbeddings(args);
        if (args[0] == "import-folder")
            await ImportFileAndCreateEmbeddingsInFolder(args);
        else if (args[0] == "search")
            await PerformSearch(args);
        else
            Console.WriteLine("Invalid command");
    }

    private static async Task ImportFileAndCreateEmbeddingsInFolder(string[] args)
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

    private static async Task PerformSearch(string[] args)
    {
        var query = args[1];
        var collection = await InitializeVectorStore();

        var searchVector = await _textEmbeddingGenerationService.GenerateEmbeddingAsync(query);
        var resultcount = 5;
        bool showcontent = args.Contains("-content", StringComparer.InvariantCultureIgnoreCase);

        var searchResult = await collection.VectorizedSearchAsync(
            searchVector,
            new VectorSearchOptions { Top = resultcount, IncludeVectors = false }
        );
        var searchResults = searchResult.Results.ToBlockingEnumerable().Select(x => x).ToList();
        if (searchResults.Count == 0)
        {
            Console.WriteLine("No results");
            return;
        }
        if (args.Contains("-rag"))
        {
            var contexts = searchResults.Select(x => x.Record.Content).ToArray();
            var books = searchResults.Select(x => x.Record.Book).Distinct().ToArray();
            Console.WriteLine(
                $"Asking GPT with {resultcount} contexts. from these {books.Length} books:"
            );
            foreach (var book in books)
            {
                Console.WriteLine($" - {book}");
            }
            var response = await _chatService.AskRaggedQuestion(query, contexts.ToArray());

            Console.WriteLine("--------- Answer -------------");
            Console.WriteLine(response);
        }
        else
        {
            var result = searchResults[0];

            Console.WriteLine($"Search score: {result.Score}");
            Console.WriteLine($"Key: {result.Record.Key}");
            Console.WriteLine($"Book: {result.Record.Book}");
            Console.WriteLine($"Chapter: {result.Record.Chapter}");
            Console.WriteLine($"Page: {result.Record.PageNumber}");
            if (showcontent)
            {
                Console.WriteLine($"Content: \n{result.Record.Content}");
            }

            var bookfolder = raggedBookConfig.PdfFolder;
            var fileLink =
                $"file://{bookfolder}{result.Record.Book}.pdf#page={result.Record.PageNumber}";

            // url encode the file link
            fileLink = fileLink.Replace(" ", "%20");
            Console.WriteLine(fileLink);
            Console.WriteLine("=========");
            Console.WriteLine();
            if (args.Contains("-open"))
            {
                Process.Start(raggedBookConfig.ChromeExePath, fileLink);
            }
            //}
        }
    }

    private static async Task ImportFileAndCreateEmbeddings(string[] args)
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

            foreach (var (index, paragraph) in paragraphs.Select((x, index) => (index, x)))
            {
                var embedding = embeddings[index];
                var chunk = new ContentChunk
                {
                    Key = Guid.NewGuid(),
                    Book = bookname,
                    Chapter = bookmarktree.GetChapterPath(page.pagenumber),
                    PageNumber = page.pagenumber,
                    Content = paragraph,
                    ContentEmbedding = embedding, //new ReadOnlyMemory<float>(embedding)
                };
                chunks.Add(chunk);
            }
        }

        IVectorStoreRecordCollection<ulong, ContentChunk> collection =
            await InitializeVectorStore();

        var keys = new List<ulong>();
        Console.WriteLine("Adding records to Qdrant");
        await foreach (var key in collection.UpsertBatchAsync(chunks))
        {
            keys.Add(key);
        }
        Console.WriteLine($"Added {keys.Count} records to Qdrant for {bookname}");
    }

    private static async Task<
        IVectorStoreRecordCollection<ulong, ContentChunk>
    > InitializeVectorStore()
    {
        // Create a Qdrant VectorStore object
        var vectorStore = new QdrantVectorStore(new QdrantClient("localhost"));

        // Choose a collection from the database and specify the type of key and record stored in it via Generic parameters.
        var collection = vectorStore.GetCollection<ulong, ContentChunk>("skcontent");
        await collection.CreateCollectionIfNotExistsAsync();

        return collection;
    }

    private static Kernel InitializeKernel()
    {
        //Create Kernel builder
        var builder = Kernel.CreateBuilder();

        var apiKey = azureOpenAiSettings.ApiKey;
        var azureEndpoint = azureOpenAiSettings.Endpoint;

        builder.AddOllamaTextEmbeddingGeneration(
            "nomic-embed-text",
            //"mxbai-embed-large",
            raggedBookConfig.OllamaUrl
        );
        builder.AddAzureOpenAIChatCompletion(azureOpenAiSettings.ModelId, azureEndpoint, apiKey);

        var kernel = builder.Build();
        _textEmbeddingGenerationService =
            kernel.GetRequiredService<ITextEmbeddingGenerationService>();

        return kernel;
    }
}

#pragma warning disable SKEXP0010
#pragma warning disable SKEXP0001
#pragma warning disable SKEXP0050
#pragma warning disable SKEXP0070

using System.Diagnostics;
using Embedder;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.Qdrant;
using Microsoft.SemanticKernel.Embeddings;
using Microsoft.SemanticKernel.Text;
using Qdrant.Client;
using Kernel = Microsoft.SemanticKernel.Kernel;

internal class Program
{
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
            await ImportFileAndCreateEmbeddings(args, kernel);
        if (args[0] == "import-folder")
            await ImportFileAndCreateEmbeddingsInFolder(args, kernel);
        else if (args[0] == "search")
            await SearchEmbeddings(args, kernel);
        else
            Console.WriteLine("Invalid command");
    }

    private static async Task ImportFileAndCreateEmbeddingsInFolder(string[] args, Kernel kernel)
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
            await ImportFileAndCreateEmbeddings(new[] { "import-file", file }, kernel);
        }
    }

    private static async Task SearchEmbeddings(string[] args, Kernel kernel)
    {
        var query = args[1];
        var collection = await InitializeVectorStore();
        var textEmbeddingGenerationService =
            kernel.GetRequiredService<ITextEmbeddingGenerationService>();

        var searchVector = await textEmbeddingGenerationService.GenerateEmbeddingAsync(query);
        var resultcount = 1;
        bool showcontent = args.Contains("-content", StringComparer.InvariantCultureIgnoreCase);

        var searchResult = await collection.VectorizedSearchAsync(
            searchVector,
            new VectorSearchOptions { Top = resultcount, IncludeVectors = false }
        );

        await foreach (var result in searchResult.Results)
        {
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
        }
    }

    private static async Task ImportFileAndCreateEmbeddings(string[] args, Kernel kernel)
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
        var textEmbeddingGenerationService =
            kernel.GetRequiredService<ITextEmbeddingGenerationService>();

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

            var embeddings = await textEmbeddingGenerationService.GenerateEmbeddingsAsync(
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
        return kernel;
    }
}

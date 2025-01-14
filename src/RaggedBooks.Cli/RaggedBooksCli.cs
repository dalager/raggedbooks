using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RaggedBooks.Core;

namespace RaggedBooks.Cli;

public class RaggedBooksCli(
    FileImportService fileImportService,
    VectorSearchService vectorSearchService,
    ChatService chatService,
    IOptions<RaggedBookConfig> raggedBookConfig,
    ILogger<RaggedBooksCli> logger
)
{
    private readonly RaggedBookConfig _raggedBookConfig = raggedBookConfig.Value;

    public async Task Run(string[] args)
    {
        logger.LogInformation("Hej");
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
            await fileImportService.ImportFileAndCreateEmbeddings(args);
        else if (args[0] == "import-folder")
            await fileImportService.ImportFileAndCreateEmbeddingsInFolder(args);
        else if (args[0] == "search")
            await PerformSearch(args);
        else
            Console.WriteLine("Invalid command");
    }

    private async Task PerformSearch(string[] args)
    {
        var query = args[1];
        var resultcount = 5;

        var searchResult = await vectorSearchService.SearchVectorStore(query);
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

            var response = await chatService.AskRaggedQuestion(query, contexts.ToArray());

            Console.WriteLine("--------- Answer -------------");
            Console.WriteLine(response);
        }
        else
        {
            var result = searchResults[0];
            bool showcontent = args.Contains("-content", StringComparer.InvariantCultureIgnoreCase);

            Console.WriteLine($"Search score: {result.Score}");
            Console.WriteLine($"Key: {result.Record.Id}");
            Console.WriteLine($"Book: {result.Record.Book}");
            Console.WriteLine($"Chapter: {result.Record.Chapter}");
            Console.WriteLine($"Page: {result.Record.PageNumber}");
            if (showcontent)
            {
                Console.WriteLine($"Content: \n{result.Record.Content}");
            }

            var bookfolder = _raggedBookConfig.PdfFolder;
            var fileLink =
                $"file://{bookfolder}{result.Record.BookFilename}#page={result.Record.PageNumber}";

            // url encode the file link
            fileLink = fileLink.Replace(" ", "%20");
            Console.WriteLine(fileLink);
            Console.WriteLine("=========");
            Console.WriteLine();
            if (args.Contains("-open"))
            {
                Process.Start(_raggedBookConfig.ChromeExePath, fileLink);
            }
        }
    }
}

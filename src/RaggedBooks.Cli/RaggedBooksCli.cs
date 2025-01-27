using System.Diagnostics;
using Microsoft.Extensions.Logging;
using RaggedBooks.Core.Chat;
using RaggedBooks.Core.Configuration;
using RaggedBooks.Core.SemanticSearch;
using RaggedBooks.Core.TextExtraction;

namespace RaggedBooks.Cli;

public class RaggedBooksCli(
    FileImportService fileImportService,
    VectorSearchService vectorSearchService,
    ChatService chatService,
    RaggedBookConfig raggedBookConfig,
    ILogger<RaggedBooksCli> logger
)
{
    public async Task Run(string[] args)
    {
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

        switch (args[0])
        {
            case "import-file":
                await fileImportService.ImportFileAndCreateEmbeddings(args);
                break;
            case "import-folder":
                await fileImportService.ImportFolder(args);
                break;
            case "search":
                await PerformSearch(args);
                break;
            default:
                Console.WriteLine("Invalid command");
                break;
        }
    }

    private async Task PerformSearch(string[] args)
    {
        logger.LogInformation("Performing search");
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
            var textChunks = searchResults.Select(x => x.Record.Content).ToArray();
            var books = searchResults.Select(x => x.Record.Book).Distinct().ToArray();
            logger.LogInformation(
                "Asking {Model} with {Resultcount} contexts. from these {BookCount} books:",
                raggedBookConfig.ChatCompletionModel,
                resultcount,
                books.Length
            );
            foreach (var book in books)
            {
                logger.LogInformation(" - {Book}", book);
            }

            var response = await chatService.AskRaggedQuestion(query, [.. textChunks]);

            logger.LogInformation("--------- Answer -------------");
            logger.LogInformation("{Answer}", response);
        }
        else
        {
            var result = searchResults[0];
            var showcontent = args.Contains("-content", StringComparer.InvariantCultureIgnoreCase);

            logger.LogInformation("Search score: {Score}", result.Score);
            logger.LogInformation("Key: {Id}", result.Record.Id);
            logger.LogInformation("Book: {Book}", result.Record.Book);
            logger.LogInformation("Chapter: {Chapter}", result.Record.Chapter);
            logger.LogInformation("Page: {PageNumber}", result.Record.PageNumber);
            if (showcontent)
            {
                logger.LogInformation("Content: \n{Content}", result.Record.Content);
            }

            var bookfolder = raggedBookConfig.PdfFolder;
            var fileLink =
                $"file://{bookfolder}{result.Record.BookFilename}#page={result.Record.PageNumber}";

            // url encode the file link
            fileLink = fileLink.Replace(" ", "%20");
            if (args.Contains("-open"))
            {
                logger.LogInformation("Opening {FileLink}", fileLink);
                Process.Start(raggedBookConfig.ChromeExePath, fileLink);
            }
        }
    }
}

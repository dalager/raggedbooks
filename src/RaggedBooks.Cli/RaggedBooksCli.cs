using System.Diagnostics;
using Microsoft.Extensions.Logging;
using RaggedBooks.Core;
using RaggedBooks.Core.Chat;
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

        if (args[0] == "import-file")
            await fileImportService.ImportFileAndCreateEmbeddings(args);
        else if (args[0] == "import-folder")
            await fileImportService.ImportFolder(args);
        else if (args[0] == "search")
            await PerformSearch(args);
        else
            Console.WriteLine("Invalid command");
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
            var contexts = searchResults.Select(x => x.Record.Content).ToArray();
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

            var response = await chatService.AskRaggedQuestion(query, contexts.ToArray());

            logger.LogInformation("--------- Answer -------------");
            logger.LogInformation(response);
        }
        else
        {
            var result = searchResults[0];
            bool showcontent = args.Contains("-content", StringComparer.InvariantCultureIgnoreCase);

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

using System.Diagnostics;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Embedder;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RaggedBooks.Core;

namespace RaggedBooks.Client.ViewModels;

public class MainViewModel : ObservableObject, IMainViewModel
{
    private readonly VectorSearchService _vectorSearchService;
    private readonly ChatService _chatService;
    private readonly ILogger<MainViewModel> _logger;
    private ICommand _lookupCommand = null!;
    private ICommand _searchCommand = null!;
    private string _query = string.Empty;
    private string _searchResults = string.Empty;
    private string _statusText = DefaultStatusText;
    private readonly RaggedBookConfig _raggedConfig;
    private const string DefaultStatusText = "";

    public MainViewModel(
        VectorSearchService vectorSearchService,
        ChatService chatService,
        IOptions<RaggedBookConfig> raggedConfigOptions,
        ILogger<MainViewModel> logger
    )
    {
        _vectorSearchService = vectorSearchService;
        _chatService = chatService;
        _logger = logger;
        _raggedConfig = raggedConfigOptions.Value;
    }

    public ICommand SearchCommand => _searchCommand ??= new AsyncRelayCommand(ExecuteSearchAsync);
    public ICommand LookupCommand => _lookupCommand ??= new AsyncRelayCommand(ExecuteLookupAsync);

    private async Task ExecuteLookupAsync()
    {
        _logger.LogInformation("Executing lookup");
        StatusText = "Semantic search...";
        try
        {
            var searchResult = await _vectorSearchService.SearchVectorStore(Query);
            var searchResults = searchResult.Results.ToBlockingEnumerable().Select(x => x).ToList();
            var result = searchResults[0];
            var bookfolder = _raggedConfig.PdfFolder;
            var fileLink =
                $"file://{bookfolder}{result.Record.Book}.pdf#page={result.Record.PageNumber}";
            fileLink = fileLink.Replace(" ", "%20");
            Process.Start(_raggedConfig.ChromeExePath, fileLink);
        }
        catch (Exception e)
        {
            SearchResults = "Error: " + e.Message;
        }
        finally
        {
            StatusText = DefaultStatusText;
        }
    }

    public string Query
    {
        get => _query;
        set
        {
            _query = value;
            OnPropertyChanged();
        }
    }

    public string StatusText
    {
        get => _statusText;
        set
        {
            _statusText = value;
            OnPropertyChanged();
        }
    }

    public string SearchResults
    {
        get => _searchResults;
        set
        {
            _searchResults = value;
            OnPropertyChanged();
        }
    }

    private async Task ExecuteSearchAsync()
    {
        _logger.LogInformation("Executing search");
        try
        {
            StatusText = "Semantic search...";

            var searchResult = await _vectorSearchService.SearchVectorStore(Query);
            var searchResults = searchResult.Results.ToBlockingEnumerable().Select(x => x).ToList();

            var contexts = searchResults.Select(x => x.Record.Content).ToArray();
            StatusText = "Asking LLM...";
            var response = await _chatService.AskRaggedQuestion(Query, contexts.ToArray());
            SearchResults = response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing search");
            SearchResults = "Error executing search";
        }
        finally
        {
            StatusText = DefaultStatusText;
        }

        // Do something with the results
    }
}

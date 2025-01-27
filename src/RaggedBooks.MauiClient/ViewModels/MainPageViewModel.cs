using System.Diagnostics;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Markdig;
using Microsoft.Extensions.Logging;
using RaggedBooks.Core;
using RaggedBooks.Core.Chat;
using RaggedBooks.Core.Configuration;
using RaggedBooks.Core.SemanticSearch;

namespace RaggedBooks.MauiClient.ViewModels
{
    public interface IMainPageViewModel
    {
        ICommand SearchCommand { get; }
        ICommand LookupCommand { get; }
        string Query { get; set; }
        string StatusText { get; set; }
        bool IsLoading { get; set; }
        HtmlWebViewSource HtmlSearchResults { get; set; }
        public ICommand OpenQdrantDashboard { get; }
        Task LoadModelsAsync();
    }

    public class MainPageViewModel : ObservableObject, IMainPageViewModel
    {
        private readonly VectorSearchService _vectorSearchService;
        private readonly ChatService _chatService;
        private readonly RaggedBookConfig _raggedConfig;
        private readonly OllamaModelManager _ollamaManager;
        private readonly ILogger<MainPageViewModel> _logger;
        private ICommand _searchCommand = null!;
        private ICommand _lookupCommand = null!;
        private ICommand _openQdrantDashboard = null!;
        private string _query = string.Empty;
        private const string defaultHtml =
            "<h1>Ragged Books</h1><p>Search your book collection with a little help from your AI friends</p><ul><li>Click on 'Go' to jump directly to the first matching page.</li>"
            + "<li>Click on 'Ask' to use the Chat Completion Model to summarize the matching content.</li></ul>";
        private HtmlWebViewSource _webViewSource;
        private string _statusText = defaultStatusText;
        private const string defaultStatusText = "";

        public ICommand SearchCommand =>
            _searchCommand ??= new AsyncRelayCommand(ExecuteSearchAsync);
        public ICommand LookupCommand =>
            _lookupCommand ??= new AsyncRelayCommand(ExecuteLookupAsync);

        public ICommand OpenQdrantDashboard =>
            _openQdrantDashboard ??= new RelayCommand(() =>
            {
                Process.Start(_raggedConfig.ChromeExePath, _raggedConfig.QdrantUrl + "dashboard");
            });

        public MainPageViewModel(
            VectorSearchService vectorSearchService,
            ChatService chatService,
            RaggedBookConfig raggedConfig,
            OllamaModelManager ollamaManager,
            ILogger<MainPageViewModel> logger
        )
        {
            _vectorSearchService = vectorSearchService;
            _chatService = chatService;
            _raggedConfig = raggedConfig;
            _ollamaManager = ollamaManager;
            _logger = logger;
            _webViewSource = new HtmlWebViewSource() { Html = WrapInHtml(defaultHtml) };
            _markdownConverter = new MarkdownToHtmlConverter();
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
        private bool _isLoading;
        private readonly MarkdownToHtmlConverter _markdownConverter;

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (_isLoading != value)
                {
                    _isLoading = value;
                    OnPropertyChanged(nameof(IsLoading));
                }
            }
        }
        public HtmlWebViewSource HtmlSearchResults
        {
            get => _webViewSource;
            set
            {
                _webViewSource = value;
                OnPropertyChanged();
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

        private async Task ExecuteLookupAsync()
        {
            _logger.LogInformation("Executing lookup");
            StatusText = "Semantic search...";
            try
            {
                var searchResult = await _vectorSearchService.SearchVectorStore(Query);
                var searchResults = searchResult
                    .Results.ToBlockingEnumerable()
                    .Select(x => x)
                    .ToList();
                var result = searchResults[0];
                var bookfolder = _raggedConfig.PdfFolder;
                var fileLink =
                    $"file://{bookfolder}{result.Record.BookFilename}#page={result.Record.PageNumber}";
                fileLink = fileLink.Replace(" ", "%20");
                Process.Start(_raggedConfig.ChromeExePath, fileLink);
            }
            catch (Exception e)
            {
                DisplayHtmlInWebview(WrapInHtml("Error: " + e.Message));
            }
            finally
            {
                StatusText = defaultStatusText;
            }
        }

        private async Task ExecuteSearchAsync()
        {
            _logger.LogInformation("Executing search");
            var stopwatch = Stopwatch.StartNew();
            try
            {
                StatusText = "Semantic search...";

                var searchResult = await _vectorSearchService.SearchVectorStore(Query);
                var searchResults = searchResult
                    .Results.ToBlockingEnumerable()
                    .Select(x => x)
                    .ToList();

                var matchingRecords = searchResults.Select(x => x.Record).ToArray();
                var contexts = new List<string>();
                foreach (var record in matchingRecords)
                {
                    var text =
                        $"{record.Content}{Environment.NewLine}(source: {record.Book} - {record.Chapter})";

                    contexts.Add(text.Trim());
                }
                var usedModel = _raggedConfig.UseLocalChatModel
                    ? _raggedConfig.ChatCompletionModel + $" ({_raggedConfig.OllamaUrl.Host})"
                    : "gpt-4o";

                StatusText = $"Asking {usedModel}...";

                var response = await _chatService.AskRaggedQuestion(Query, [.. contexts]);

                var htmlBody = _markdownConverter.ConvertToHtml(response);

                DisplayHtmlInWebview(WrapInHtml(htmlBody));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing search");
                DisplayHtmlInWebview(
                    WrapInHtml(
                        $"""
                        **Error executing search**:
                         
                         {ex.Message}
                        
                        """
                    )
                );
            }
            finally
            {
                StatusText = $"Completed in {Math.Round(stopwatch.Elapsed.TotalSeconds, 2)}s";
            }
        }

        public void DisplayHtmlInWebview(string html)
        {
            HtmlSearchResults = new HtmlWebViewSource { Html = html };
        }

        public static string WrapInHtml(string inputString)
        {
            var htmlWrapper =
                "<!DOCTYPE html>"
                + "<html>"
                + "<head>"
                + "<meta charset=\"utf-8\">"
                + "<link rel=\"stylesheet\" href=\"/MarkDownStyles.css\"/>"
                + "</head>"
                + "<body>"
                + inputString
                + "</body>"
                + "</html>";
            return htmlWrapper;
        }

        public async Task LoadModelsAsync()
        {
            IsLoading = true;

            StatusText = "Pulling required models...";
            try
            {
                await _ollamaManager.PullRequiredModels(
                    (pullingModelName) =>
                    {
                        StatusText = "Loading " + pullingModelName;
                    }
                );
            }
            finally
            {
                StatusText = "Ready 😊";
                IsLoading = false;
            }
        }
    }

    public class MarkdownToHtmlConverter
    {
        private readonly MarkdownPipeline _pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .Build();

        public string ConvertToHtml(string markdown)
        {
            return Markdown.ToHtml(markdown, _pipeline);
        }
    }
}

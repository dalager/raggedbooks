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
    public class MainPageViewModel : ObservableObject, IMainPageViewModel
    {
        private readonly IVectorSearchService _vectorSearchService;
        private readonly IChatService _chatService;
        private readonly RaggedBookConfig _raggedConfig;
        private readonly OllamaModelManager _ollamaManager;
        private readonly ILogger<MainPageViewModel> _logger;
        private readonly IRagService _ragService;
        private string _query = string.Empty;
        private const string defaultHtml =
            "<h1>Ragged Books</h1><p>Search your book collection with a little help from your AI friends</p><ul><li>Click on 'Go' to jump directly to the first matching page.</li>"
            + "<li>Click on 'Ask' to use the Chat Completion Model to summarize the matching content.</li></ul>";
        private HtmlWebViewSource _webViewSource;
        private string _statusText = defaultStatusText;
        private const string defaultStatusText = "";

        public ICommand SearchCommand { get; }

        public ICommand LookupCommand { get; }

        public ICommand OpenQdrantDashboard { get; }


        public MainPageViewModel(
            IVectorSearchService vectorSearchService,
            IChatService chatService,
            RaggedBookConfig raggedConfig,
            OllamaModelManager ollamaManager,
            ILogger<MainPageViewModel> logger,
            IRagService ragService
        )
        {
            _vectorSearchService = vectorSearchService;
            _chatService = chatService;
            _raggedConfig = raggedConfig;
            _ollamaManager = ollamaManager;
            _logger = logger;
            _ragService = ragService;
            _webViewSource = new HtmlWebViewSource() { Html = WrapInHtml(defaultHtml) };
            _markdownConverter = new MarkdownToHtmlConverter();
            SearchCommand = new AsyncRelayCommand(ExecuteSearchAsync);
            LookupCommand = new AsyncRelayCommand(ExecuteLookupAsync);
            OpenQdrantDashboard = new RelayCommand(() => { Process.Start(_raggedConfig.ChromeExePath, _raggedConfig.QdrantUrl + "dashboard"); });
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

        private string _ollamaStatus = string.Empty;

        public string OllamaStatus
        {
            get => _ollamaStatus;
            private set
            {
                _ollamaStatus = value;
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
                    OnPropertyChanged();
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
                StatusText = "Searching...";
                var markdownString = await _ragService.Search(Query);

                var htmlBody = _markdownConverter.ConvertToHtml(markdownString);

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

        private void DisplayHtmlInWebview(string html)
        {
            HtmlSearchResults = new HtmlWebViewSource { Html = html };
        }

        private static string WrapInHtml(string inputString)
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
            var ollamaState = " 🟢";
            StatusText = "Pulling required models...";
            try
            {
                await _ollamaManager.PullRequiredModels(
                    (pullingModelName) => { StatusText = "Loading " + pullingModelName; }
                );
                StatusText = "Ready 😊";
            }
            catch (Exception)
            {
                StatusText = "Could not access ollama " + "😵‍💫";
                ollamaState = " 🔴";
            }
            finally
            {
                OllamaStatus = _raggedConfig.ChatCompletionModel + "/" + _raggedConfig.OllamaUrl.Host + ollamaState;
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

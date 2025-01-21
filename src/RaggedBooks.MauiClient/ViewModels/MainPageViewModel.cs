using System.Diagnostics;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Markdig;
using Microsoft.Extensions.Logging;
using RaggedBooks.Core;
using RaggedBooks.Core.Chat;
using RaggedBooks.Core.SemanticSearch;

namespace RaggedBooks.MauiClient.ViewModels
{
    public class MainPageViewModel : ObservableObject
    {
        private readonly VectorSearchService _vectorSearchService;
        private readonly ChatService _chatService;
        private readonly RaggedBookConfig _raggedConfig;
        private readonly OllamaModelManager _ollamaManager;
        private readonly ILogger<MainPageViewModel> _logger;
        private ICommand _searchCommand = null!;
        private ICommand _lookupCommand = null!;
        private readonly ICommand _focusTextCommand = null!;
        private string _query = string.Empty;
        private const string DefaultHtml =
            "<h1>Ragged Books</h1><p>Search your book collection with a little help from your AI friends</p><ul><li>Click on 'Go' to jump directly to the first matching page.</li>"
            + "<li>Click on 'Ask' to use the Chat Completion Model to summarize the matching content.</li></ul>";
        private HtmlWebViewSource _webViewSource;
        private string _statusText = DefaultStatusText;
        private const string DefaultStatusText = "";

        public ICommand SearchCommand =>
            _searchCommand ??= new AsyncRelayCommand(ExecuteSearchAsync);
        public ICommand LookupCommand =>
            _lookupCommand ??= new AsyncRelayCommand(ExecuteLookupAsync);

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
            get
            {
                if (_webViewSource == null)
                {
                    _webViewSource = new HtmlWebViewSource { Html = WrapInHtml(DefaultHtml) };
                }
                return _webViewSource;
            }
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
                StatusText = DefaultStatusText;
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
                    ? _raggedConfig.ChatCompletionModel + " (local)"
                    : "gpt-4o";

                StatusText = $"Asking {usedModel}...";

                var response = await _chatService.AskRaggedQuestion(Query, contexts.ToArray());

                var htmlBody = RenderMarkdownToHtml(response);

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

            // Do something with the results
        }

        public void DisplayHtmlInWebview(string html)
        {
            HtmlSearchResults = new HtmlWebViewSource { Html = html };
        }

        public string WrapInHtml(string inputString)
        {
            string finalHtml =
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
            return finalHtml;
        }

        private string RenderMarkdownToHtml(string markdown)
        {
            var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
            return Markdown.ToHtml(markdown, pipeline);
        }

        public async Task LoadModelsAsync()
        {
            IsLoading = true;

            StatusText = "Pulling required models...";
            try
            {
                // For illustration, pretend we have multiple steps.
                // We can periodically update Progress here, but that requires a thread-safe call to OnPropertyChanged.
                await _ollamaManager.PullRequiredModels(
                    (pullingModelName) =>
                    {
                        StatusText = "Loading " + pullingModelName;
                    }
                );
                //progressValue =>
                //{
                // // update progress
                // // to avoid cross-thread issues, capture it in a local var, then use Dispatcher
                // Application.Current.Dispatcher.Invoke(() =>
                // {
                //  Progress = progressValue;
                // });
                //});
            }
            finally
            {
                StatusText = "Ready 😊";
                IsLoading = false;
            }
        }
    }
}

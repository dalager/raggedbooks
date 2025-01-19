﻿using System.Diagnostics;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using RaggedBooks.Core;
using RaggedBooks.Core.Chat;
using RaggedBooks.Core.SemanticSearch;

namespace RaggedBooks.Client.ViewModels;

public class MainViewModel : ObservableObject, IMainViewModel
{
    private readonly VectorSearchService _vectorSearchService;
    private readonly ChatService _chatService;
    private readonly ILogger<MainViewModel> _logger;
    private ICommand _searchCommand = null!;
    private ICommand _lookupCommand = null!;
    private ICommand _focusTextCommand = null!;
    private string _query = string.Empty;
    private string _searchResults = string.Empty;
    private string _statusText = DefaultStatusText;
    private readonly RaggedBookConfig _raggedConfig;
    private const string DefaultStatusText = "";

    public MainViewModel(
        VectorSearchService vectorSearchService,
        ChatService chatService,
        RaggedBookConfig raggedConfigOptions,
        OllamaModelManager ollamaManager,
        ILogger<MainViewModel> logger
    )
    {
        _vectorSearchService = vectorSearchService;
        _chatService = chatService;
        _logger = logger;
        _raggedConfig = raggedConfigOptions;
        _ollamaManager = ollamaManager;
    }

    public ICommand SearchCommand => _searchCommand ??= new AsyncRelayCommand(ExecuteSearchAsync);
    public ICommand LookupCommand => _lookupCommand ??= new AsyncRelayCommand(ExecuteLookupAsync);

    // Command that triggers focus
    public ICommand FocusTextCommand => _focusTextCommand ??= new AsyncRelayCommand(FocusText);
    private readonly OllamaModelManager _ollamaManager;
    private bool _isLoading;
    private int _progress; // or double if you prefer

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

    public int Progress
    {
        get => _progress;
        set
        {
            if (_progress != value)
            {
                _progress = value;
                OnPropertyChanged(nameof(Progress));
            }
        }
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
                $"file://{bookfolder}{result.Record.BookFilename}#page={result.Record.PageNumber}";
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
        var stopwatch = Stopwatch.StartNew();
        try
        {
            StatusText = "Semantic search...";

            var searchResult = await _vectorSearchService.SearchVectorStore(Query);
            var searchResults = searchResult.Results.ToBlockingEnumerable().Select(x => x).ToList();

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

            SearchResults = response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing search");
            SearchResults = $"""
                **Error executing search**:
                 
                 {ex.Message}
                
                """;
        }
        finally
        {
            StatusText = $"Completed in {Math.Round(stopwatch.Elapsed.TotalSeconds, 2)}s";
        }

        // Do something with the results
    }

    private bool _isTextBoxFocused;

    // Bind this to the attached property in XAML
    public bool IsTextBoxFocused
    {
        get => _isTextBoxFocused;
        set
        {
            if (_isTextBoxFocused != value)
            {
                _isTextBoxFocused = value;
                OnPropertyChanged(nameof(IsTextBoxFocused));
            }
        }
    }

    private Task FocusText()
    {
        // Set IsTextBoxFocused to true (View sees this and sets focus).
        IsTextBoxFocused = true;
        return Task.CompletedTask;
    }
}

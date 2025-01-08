using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Embedder;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using RaggedBooks.Core;

namespace RaggedBooks.Client.ViewModels;

public class MainViewModel : ObservableObject, IMainViewModel
{
	private readonly VectorSearchService _vectorSearchService;
	private readonly ChatService _chatService;
	private readonly Kernel _kernel;
	private readonly ILogger<MainViewModel> _logger;

	public MainViewModel(
		VectorSearchService vectorSearchService,
		ChatService chatService,
		Kernel kernel,
		ILogger<MainViewModel> logger
	)
	{
		_vectorSearchService = vectorSearchService;
		_chatService = chatService;
		_kernel = kernel;
		_logger = logger;
	}

	private ICommand _searchCommand;
	private string _query;

	public ICommand SearchCommand => _searchCommand ??= new AsyncRelayCommand(ExecuteSearchAsync);

	public string Query
	{
		get => _query;
		set
		{
			_query = value;
			OnPropertyChanged();
		}
	}

	private string _searchResults;
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
			var searchResult = await _vectorSearchService.SearchVectorStore(Query);
			var searchResults = searchResult.Results.ToBlockingEnumerable().Select(x => x).ToList();

			//SearchResults = results
			//    .Results.ToBlockingEnumerable(CancellationToken.None)
			//    .FirstOrDefault()
			//    .Record.Content;


			var contexts = searchResults.Select(x => x.Record.Content).ToArray();
			//var books = searchResults.Select(x => x.Record.Book).Distinct().ToArray();
			//foreach (var book in books)
			//{
			// Console.WriteLine($" - {book}");
			//}

			var response = await _chatService.AskRaggedQuestion(Query, contexts.ToArray());
			SearchResults = response;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error executing search");
			SearchResults = "Error executing search";
		}

		// Do something with the results
	}
}
using System.Windows.Input;

namespace RaggedBooks.MauiClient.ViewModels;

public interface IMainPageViewModel
{
    ICommand SearchCommand { get; }
    ICommand LookupCommand { get; }
    string Query { get; set; }
    string StatusText { get; set; }
    bool IsLoading { get; set; }
    HtmlWebViewSource HtmlSearchResults { get; set; }
    public ICommand OpenQdrantDashboard { get; }
    public string OllamaStatus { get; }
    Task LoadModelsAsync();
}
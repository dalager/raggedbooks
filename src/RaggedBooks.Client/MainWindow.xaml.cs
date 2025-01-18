using System.Windows;
using RaggedBooks.Client.ViewModels;

namespace RaggedBooks.Client;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly IMainViewModel _viewModel;

    public MainWindow(IMainViewModel viewModel)
    {
        _viewModel = viewModel;
        InitializeComponent();
        DataContext = viewModel;
    }

    protected override void OnContentRendered(EventArgs e)
    {
        base.OnContentRendered(e);
        _ = _viewModel.LoadModelsAsync();
    }
}

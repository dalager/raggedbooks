using System.Windows;
using RaggedBooks.Client.ViewModels;

namespace RaggedBooks.Client;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow(IMainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}

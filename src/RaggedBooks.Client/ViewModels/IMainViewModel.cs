using System.Windows.Input;

namespace RaggedBooks.Client.ViewModels;

public interface IMainViewModel
{
    ICommand SearchCommand { get; }
    ICommand LookupCommand { get; }
}

using RaggedBooks.MauiClient.ViewModels;

namespace RaggedBooks.MauiClient
{
    public partial class MainPage : ContentPage
    {
        private readonly IMainPageViewModel _viewModel;

        public MainPage(IMainPageViewModel viewModel)
        {
            _viewModel = viewModel;
            BindingContext = viewModel;
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigatedToEventArgs args)
        {
            base.OnNavigatedTo(args);
            _ = _viewModel.LoadModelsAsync();
        }
    }
}

using RaggedBooks.MauiClient.ViewModels;

namespace RaggedBooks.MauiClient
{
    public partial class MainPage : ContentPage
    {
        private readonly MainPageViewModel _viewModel;

        public MainPage(MainPageViewModel viewModel)
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

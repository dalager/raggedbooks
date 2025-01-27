using RaggedBooks.Core;
using RaggedBooks.MauiClient.ViewModels;

namespace RaggedBooks.MauiClient
{
    public static class RegistrationExtensions
    {
        public static MauiAppBuilder RegisterViewModels(this MauiAppBuilder mauiAppBuilder)
        {
            mauiAppBuilder.Services.AddSingleton<IMainPageViewModel, MainPageViewModel>();
            return mauiAppBuilder;
        }

        public static MauiAppBuilder RegisterAppServices(this MauiAppBuilder mauiAppBuilder)
        {
            var services = ServiceInitialization.CreateServices(mauiAppBuilder.Configuration);

            // merge servicecollections
            foreach (var service in services)
            {
                mauiAppBuilder.Services.Add(service);
            }

            return mauiAppBuilder;
        }

        public static MauiAppBuilder RegisterViews(this MauiAppBuilder mauiAppBuilder)
        {
            // Omitted for brevity...
            return mauiAppBuilder;
        }
    }
}

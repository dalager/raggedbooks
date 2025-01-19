using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace RaggedBooks.MauiClient
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("Appsettings.json", optional: false, reloadOnChange: true)
                .Build();
            var builder = MauiApp.CreateBuilder();
            builder.Configuration.AddConfiguration(configuration);

            builder
                .UseMauiApp<App>()
                .RegisterAppServices()
                .RegisterViewModels()
                .RegisterViews()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}

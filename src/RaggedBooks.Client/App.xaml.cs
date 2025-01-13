using System.IO;
using System.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RaggedBooks.Client.ViewModels;
using RaggedBooks.Core;

namespace RaggedBooks.Client;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private ServiceProvider _serviceProvider = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        var configFolder = new DirectoryInfo(Path.Combine(@"..\..\..\..\Config\"));
        var configuration = new ConfigurationBuilder()
            //.SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile(
                Path.Combine(configFolder.FullName, "Appsettings.json"),
                optional: false,
                reloadOnChange: true
            )
            .Build();

        var serviceCollection = ServiceInitialization.CreateServices(configuration);
        serviceCollection.AddSingleton<IMainViewModel, MainViewModel>();
        serviceCollection.AddSingleton<MainWindow>();

        _serviceProvider = serviceCollection.BuildServiceProvider();
        _serviceProvider.GetRequiredService<MainWindow>().Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (_serviceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}

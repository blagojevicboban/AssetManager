using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SredstvaApp.ViewModels;
using SredstvaData;

namespace SredstvaApp;

public partial class App : Application
{
    public static IHost? AppHost { get; private set; }

    public App()
    {
        AppHost = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                services.AddDbContext<SredstvaDbContext>();
                services.AddSingleton<MainWindow>();
                services.AddSingleton<MainWindowViewModel>();
            })
            .Build();
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        await AppHost!.StartAsync();
        
        var dbContext = AppHost.Services.GetRequiredService<SredstvaDbContext>();
        dbContext.Database.EnsureCreated(); // Ensure DB is created on startup

        var mainWindow = AppHost.Services.GetRequiredService<MainWindow>();
        mainWindow.Show();

        base.OnStartup(e);
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        await AppHost!.StopAsync();
        base.OnExit(e);
    }
}

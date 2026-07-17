using System;
using System.IO;
using System.Linq;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SredstvaApp.ViewModels;
using SredstvaData;
using SredstvaData.Models;
using Velopack;

namespace SredstvaApp;

public partial class App : Application
{
    public static IHost? AppHost { get; private set; }

    public App()
    {
        // Globalni handler za neuhvaćene izuzetke na UI threadu
        DispatcherUnhandledException += (s, ex) =>
        {
            var logPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SredstvaApp", "crash.log");
            Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);
            File.AppendAllText(logPath, $"[{DateTime.Now}] UI EXCEPTION: {ex.Exception}\n\n");
            MessageBox.Show(
                $"Neočekivana greška:\n\n{ex.Exception.Message}\n\nDetalji su sačuvani u:\n{logPath}",
                "Greška aplikacije",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            ex.Handled = true;
        };

        // Handler za fatalne izuzetke na pozadinskim threadovima
        AppDomain.CurrentDomain.UnhandledException += (s, ex) =>
        {
            var logPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SredstvaApp", "crash.log");
            Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);
            File.AppendAllText(logPath, $"[{DateTime.Now}] FATAL: {ex.ExceptionObject}\n\n");
        };

        // Handler za neuhvaćene Task izuzetke (sprečava tihi crash)
        System.Threading.Tasks.TaskScheduler.UnobservedTaskException += (s, ex) =>
        {
            var logPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SredstvaApp", "crash.log");
            Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);
            File.AppendAllText(logPath, $"[{DateTime.Now}] TASK: {ex.Exception}\n\n");
            ex.SetObserved(); // Sprečava crash procesa
        };

        AppHost = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                // Singleton jer MainWindow drži DbContext tokom celog životnog veka aplikacije
                services.AddSingleton<SredstvaDbContext>(provider => SredstvaDbContext.Create(AppConfig.DbPath));
                services.AddSingleton<MainWindow>();
                services.AddSingleton<MainWindowViewModel>();
            })
            .Build();
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        VelopackApp.Build().Run();

        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
        await AppHost!.StartAsync();

        var dbContext = AppHost.Services.GetRequiredService<SredstvaDbContext>();
        // Migracije se pokrecu unutar SredstvaDbContext.Create()

        // PATCH: Ispravka za sredstva koja su ranije rashodovana a ostala su JeAktivno=true
        try
        {
            var rashodovanaIds = dbContext.Rashodi
                .Where(r => r.Kod == TipoviPromena.Rashodovanje || r.Kod == TipoviPromena.Prodaja || r.Kod == TipoviPromena.Otudjenje || r.Kod == TipoviPromena.Brisanje)
                .Select(r => r.SredstvoId)
                .Distinct()
                .ToList();
            var zaUpdate = dbContext.Sredstva.Where(s => rashodovanaIds.Contains(s.Id) && s.JeAktivno).ToList();
            if (zaUpdate.Any())
            {
                foreach (var s in zaUpdate) s.JeAktivno = false;
                dbContext.SaveChanges();
            }
        }
        catch { }

        var loginWindow = new Views.Korisnici.LoginWindow(dbContext);
        loginWindow.Show();

        base.OnStartup(e);
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        await AppHost!.StopAsync();
        base.OnExit(e);
    }
}

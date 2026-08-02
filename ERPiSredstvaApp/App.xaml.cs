using System;
using System.IO;
using System.Linq;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ERPiSredstvaApp.ViewModels;
using ERPiSredstvaData;
using ERPiSredstvaData.Models;
using Velopack;

namespace ERPiSredstvaApp;

public partial class App : Application
{
    public static IHost? AppHost { get; private set; }

    public App()
    {
        AppLog.Init();
        AppLog.RegistrujGlobalneHandlere(this);

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

        for (int i = 0; i < e.Args.Length; i++)
        {
            if (e.Args[i] == "--db-path" && i + 1 < e.Args.Length)
            {
                var customPath = e.Args[i + 1];
                if (File.Exists(customPath))
                {
                    UserSettings.Instance.ActiveDbPath = customPath;
                }
            }
        }

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
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Greška pri ažuriranju statusa rashodovanih sredstava");
        }

        var loginWindow = new Views.Korisnici.LoginWindow(dbContext);
        loginWindow.Show();

        base.OnStartup(e);
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        try
        {
            var freq = UserSettings.Instance.AutoBackupFrequency;
            if (freq == 1) // Pri svakom izlasku
            {
                Services.BackupService.Instance.NapraviAutomatskiBackup();
            }
            else if (freq == 2) // Jednom dnevno
            {
                var last = UserSettings.Instance.LastAutoBackupDate;
                if (last == null || last.Value.Date < DateTime.Now.Date)
                {
                    Services.BackupService.Instance.NapraviAutomatskiBackup();
                    UserSettings.Instance.LastAutoBackupDate = DateTime.Now;
                    UserSettings.Instance.Save();
                }
            }
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Greška pri automatskom pravljenju rezervne kopije prilikom izlaska");
        }

        await AppHost!.StopAsync();
        AppLog.Zatvori();
        base.OnExit(e);
    }
}

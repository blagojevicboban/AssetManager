using System;
using System.IO;
using SredstvaData;
using System.Linq;

namespace SredstvaApp;

public static class AppConfig
{
    public static string DefaultDbPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "sredstva.db"
    );

    public static string BazeDir => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "SredstvaApp", "Baze"
    );

    private static string? _dbPath = null;

    private static void PrilagodiNazivZajednickeBaze()
    {
        try
        {
            var bazeDir = BazeDir;
            Directory.CreateDirectory(bazeDir);

            // Stara baza može biti sredstva.db u LocalAppData
            var zajednickaDb = DefaultDbPath;
            if (File.Exists(zajednickaDb))
            {
                // Privremeno otvori bazu i pročitaj podatke o firmi
                using var db = SredstvaDbContext.Create(zajednickaDb);
                var f = db.Firme.FirstOrDefault();
                if (f != null && !string.IsNullOrWhiteSpace(f.MaticniBroj))
                {
                    var pib = !string.IsNullOrWhiteSpace(f.PIB) ? f.PIB.Trim() : f.MaticniBroj.Trim();
                    var nazivClean = string.Concat(f.Naziv.Trim().Split(Path.GetInvalidFileNameChars())).Replace(" ", "_");
                    var noviNaziv = $"firma_{pib}_{nazivClean}.db";
                    var novaPutanja = Path.Combine(bazeDir, noviNaziv);

                    // Zatvori konekcije
                    db.Dispose();
                    Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();

                    // Preimenuj fajl na disku
                    if (!File.Exists(novaPutanja))
                    {
                        File.Move(zajednickaDb, novaPutanja);
                    }
                    else
                    {
                        File.Delete(zajednickaDb);
                    }

                    // Ažuriraj aktivnu putanju
                    if (UserSettings.Instance.ActiveDbPath == zajednickaDb || string.IsNullOrEmpty(UserSettings.Instance.ActiveDbPath))
                    {
                        UserSettings.Instance.ActiveDbPath = novaPutanja;
                        UserSettings.Instance.Save();
                        _dbPath = novaPutanja;
                    }
                }
            }
        }
        catch { }
    }

    public static string DbPath
    {
        get
        {
            if (_dbPath == null)
            {
                // Pokušaj migracije iz sredstva.db u Baze\firma_...db
                PrilagodiNazivZajednickeBaze();

                var savedPath = UserSettings.Instance.ActiveDbPath;
                if (!string.IsNullOrWhiteSpace(savedPath) && File.Exists(savedPath))
                {
                    _dbPath = savedPath;
                }
                else
                {
                    var bazeDir = BazeDir;
                    Directory.CreateDirectory(bazeDir);
                    
                    var baze = Directory.GetFiles(bazeDir, "*.db");
                    if (baze.Length > 0)
                    {
                        _dbPath = baze[0];
                        UserSettings.Instance.ActiveDbPath = _dbPath;
                        UserSettings.Instance.Save();
                    }
                    else
                    {
                        _dbPath = DefaultDbPath;
                    }
                }
            }
            return _dbPath;
        }
        set
        {
            _dbPath = value;
            UserSettings.Instance.ActiveDbPath = value;
            UserSettings.Instance.Save();
        }
    }
}

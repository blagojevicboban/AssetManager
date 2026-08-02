using System;
using System.IO;
using ERPiSredstvaData;
using System.Linq;

namespace ERPiSredstvaApp;

public static class AppConfig
{
    public static string DefaultDbPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "sredstva.db"
    );

    public static string AppDataDir => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ERPiSredstvaApp"
    );

    public static string BazeDir => Path.Combine(AppDataDir, "Baze");

    /// <summary>
    /// Folderi sa podacima pod starim imenima aplikacije (pre preimenovanja u ERPi liniju).
    /// Koriste se isključivo kao izvor jednokratnog preuzimanja podataka.
    /// </summary>
    private static string[] StariAppDataDirs => new[]
    {
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SredstvaApp"),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SredstvaSystemApp")
    };

    /// <summary>Marker da je preuzimanje iz starog foldera već obavljeno.</summary>
    private static string MarkerPreuzimanja => Path.Combine(AppDataDir, "preuzeto_iz_starog_foldera.txt");

    private static string? _dbPath = null;

    /// <summary>
    /// Jednokratno preuzimanje SVIH zatečenih podataka iz foldera pod starim imenom
    /// aplikacije (%LOCALAPPDATA%\SredstvaApp) u novi (%LOCALAPPDATA%\ERPiSredstvaApp) —
    /// baze, rezervne kopije, podešavanja i logove.
    ///
    /// Preimenovanje u ERPi liniju promenilo je i ime foldera sa podacima, pa bi bez ovoga
    /// nova verzija startovala sa praznim spiskom firmi iako baze i dalje postoje na disku.
    ///
    /// Podaci se KOPIRAJU, ne premeštaju — stara instalacija ostaje upotrebljiva dok se
    /// korisnik ne uveri da je sve preneto. Da se obrisana baza ne bi vraćala pri svakom
    /// pokretanju, uspešno preuzimanje se beleži marker fajlom.
    ///
    /// Mora da se pozove PRE prvog pristupa <see cref="UserSettings.Instance"/>, jer se
    /// odmah po kopiranju premapira putanja aktivne baze.
    /// </summary>
    public static void PreuzmiStariFolderPodataka()
    {
        try
        {
            var izvori = StariAppDataDirs.Where(Directory.Exists).ToArray();
            if (izvori.Length == 0) return;

            Directory.CreateDirectory(AppDataDir);
            if (File.Exists(MarkerPreuzimanja)) return;

            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();

            int kopirano = 0;
            foreach (var izvor in izvori)
            {
                kopirano += KopirajFolder(izvor, AppDataDir);
            }

            PremapirajAktivnuBazu();

            File.WriteAllText(MarkerPreuzimanja,
                $"Podaci su preuzeti iz: {string.Join(", ", izvori)} dana {DateTime.Now:dd.MM.yyyy. HH:mm:ss}.{Environment.NewLine}" +
                $"Kopirano fajlova: {kopirano}. Original je ostao netaknut i može se obrisati ručno.{Environment.NewLine}" +
                $"Brisanje ovog fajla ponovo pokreće preuzimanje pri sledećem startu.{Environment.NewLine}");

            Serilog.Log.Information(
                "Preuzeto {Broj} fajlova iz starih foldera {Izvori} u {Odrediste}",
                kopirano, izvori, AppDataDir);
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Greška pri preuzimanju podataka iz starog foldera aplikacije");
        }
    }

    /// <summary>
    /// Rekurzivno kopira ceo sadržaj foldera. Fajl koji na odredištu već postoji se ne dira —
    /// novi podaci uvek pobeđuju nad zatečenim.
    /// </summary>
    private static int KopirajFolder(string izvor, string odrediste)
    {
        int kopirano = 0;
        Directory.CreateDirectory(odrediste);

        foreach (var fajl in Directory.GetFiles(izvor))
        {
            try
            {
                var cilj = Path.Combine(odrediste, Path.GetFileName(fajl));
                if (File.Exists(cilj))
                {
                    // Sudar imena: prazna podrazumevana baza, koju nova verzija napravi pri
                    // prvom pokretanju, ne sme da proguta istoimenu zatečenu bazu sa podacima —
                    // takva se preuzima pod sufiksom. Ostali fajlovi se preskaču.
                    if (!fajl.EndsWith(".db", StringComparison.OrdinalIgnoreCase)) continue;

                    // Ako je i zatečena baza prazna podrazumevana, nema šta da se spasava;
                    // kopija bi se samo pojavila kao lažna firma u spisku.
                    if (JePraznaPodrazumevanaBaza(fajl)) continue;

                    cilj = Path.Combine(odrediste, Path.GetFileNameWithoutExtension(fajl) + "_stara.db");
                    if (File.Exists(cilj)) continue;
                }

                File.Copy(fajl, cilj);
                kopirano++;
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning(ex, "Fajl {Fajl} nije kopiran iz starog foldera", fajl);
            }
        }

        foreach (var podfolder in Directory.GetDirectories(izvor))
        {
            kopirano += KopirajFolder(podfolder, Path.Combine(odrediste, Path.GetFileName(podfolder)));
        }

        return kopirano;
    }

    /// <summary>
    /// Vraća aktivnu bazu na firmu koja je bila otvorena pre preimenovanja — sada iz kopije
    /// u novom folderu.
    ///
    /// Nije dovoljno proveriti samo da li aktivna baza postoji: ako je nova verzija već
    /// jednom pokrenuta, ona je napravila praznu podrazumevanu bazu i upisala je kao aktivnu.
    /// Takva baza postoji, ali je prazna i ne sme da pobedi nad zatečenim podacima.
    /// </summary>
    private static void PremapirajAktivnuBazu()
    {
        try
        {
            var aktivna = UserSettings.Instance.ActiveDbPath;
            if (!string.IsNullOrWhiteSpace(aktivna) && File.Exists(aktivna) &&
                !JePraznaPodrazumevanaBaza(aktivna))
            {
                return;
            }

            var staraAktivna = StariAppDataDirs
                .Select(dir => Path.Combine(dir, "settings.json"))
                .Where(File.Exists)
                .Select(putanja => System.Text.Json.JsonSerializer
                    .Deserialize<UserSettings>(File.ReadAllText(putanja))?.ActiveDbPath)
                .FirstOrDefault(p => !string.IsNullOrWhiteSpace(p)) ?? aktivna;

            if (string.IsNullOrWhiteSpace(staraAktivna)) return;

            var kandidat = Path.Combine(BazeDir, Path.GetFileName(staraAktivna));

            // Ako je zatečena baza preuzeta pod sufiksom (zbog sudara imena), tu je i tražimo.
            if (!File.Exists(kandidat) || JePraznaPodrazumevanaBaza(kandidat))
            {
                var suSufiksom = Path.Combine(BazeDir,
                    Path.GetFileNameWithoutExtension(staraAktivna) + "_stara.db");
                if (File.Exists(suSufiksom)) kandidat = suSufiksom;
            }

            if (!File.Exists(kandidat)) return;

            UserSettings.Instance.ActiveDbPath = kandidat;
            UserSettings.Instance.Save();
            _dbPath = kandidat;

            Serilog.Log.Information("Aktivna baza premapirana na {Baza}", kandidat);
        }
        catch (Exception ex)
        {
            Serilog.Log.Warning(ex, "Aktivna baza iz starih podešavanja nije premapirana");
        }
    }

    /// <summary>
    /// Tačno kada je reč o podrazumevanoj bazi (sredstva.db) u kojoj još nema nijedne firme —
    /// takvu aplikacija sama napravi pri prvom pokretanju na praznom folderu.
    /// </summary>
    private static bool JePraznaPodrazumevanaBaza(string putanja)
    {
        if (!string.Equals(Path.GetFileName(putanja), "sredstva.db", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        try
        {
            using var conn = new Microsoft.Data.Sqlite.SqliteConnection(
                new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder
                {
                    DataSource = putanja,
                    Mode = Microsoft.Data.Sqlite.SqliteOpenMode.ReadOnly,
                    Pooling = false
                }.ConnectionString);
            conn.Open();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM Firme;";
            return Convert.ToInt64(cmd.ExecuteScalar() ?? 0L) == 0;
        }
        catch
        {
            // Baza ne postoji ili nema tabelu Firme => sveže napravljena i prazna.
            return true;
        }
    }

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
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Greška pri migraciji zajedničke baze");
        }
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

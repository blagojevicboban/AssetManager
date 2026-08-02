# 📋 Istorija izmena (Changelog) — ERPiSredstva

Sve značajne promene i novine u aplikaciji **ERPiSredstva** dokumentovane su u ovom fajlu.

Format je zasnovan na [Keep a Changelog](https://keepachangelog.com/en/1.0.0/) standardu i prati Semantic Versioning.

---

## [1.1.1] - 2026-08-02

### 🐛 Firme i baze nestale posle preimenovanja (`AppConfig`)
- **Podaci se preuzimaju iz starog foldera.** Preimenovanje u ERPi liniju promenilo je i ime foldera sa podacima (`%LOCALAPPDATA%\SredstvaApp` → `%LOCALAPPDATA%\ERPiSredstvaApp`), pa je nova verzija startovala sa praznim spiskom firmi iako sve baze i dalje stoje na disku. Pri prvom pokretanju se sada **kopira ceo stari folder** — baze, rezervne kopije, podešavanja i logovi.
- **Aktivna baza se premapira** na kopiju u novom folderu, pa se aplikacija otvara na istoj firmi kao pre.
- Podaci se **kopiraju, ne premeštaju** — stara instalacija ostaje netaknuta dok se ne uverite da je sve preneto, a stari folder možete obrisati ručno. Preuzimanje se izvršava jednom i beleži se fajlom `preuzeto_iz_starog_foldera.txt`.

## [1.1.0] - 2026-08-02

### 🏷️ Preimenovanje projekta u ERPi liniju
- **Rešenje i svi projekti preimenovani**: `SredstvaSystem.slnx` → `ERPiSredstva.slnx`, a projekti `SredstvaApp`/`SredstvaData`/`SredstvaData.Tests`/`SredstvaMigration` → `ERPiSredstvaApp`/`ERPiSredstvaData`/`ERPiSredstvaData.Tests`/`ERPiSredstvaMigration` (folderi, `.csproj` fajlovi, `namespace`-ovi i reference).
- **Repozitorijum i radni folder**: kod je premešten u `C:\ERPi\ERPiSredstva`, a `origin` pokazuje na `https://github.com/blagojevicboban/ERPiSredstva.git`.
- **Velopack `packId` je sada `ERPiSredstva`** (ranije `SredstvaSystem`), izvršni fajl je `ERPiSredstvaApp.exe`. `ERPiHub` prepoznaje i staru i novu instalaciju, pa se na računarima sa ranijom verzijom modul i dalje vidi kao instaliran.
- Ažurirani `.github/workflows/release.yml`, `.vscode` zadaci, skills dokumentacija i README.

## [1.0.56] - 2026-08-02

### 🐛 Ispravke
- **Vraćanje rezervne kopije i Single-File Publish (`IL3000`)**: Zamenjen poziv `Assembly.Location` sa `Environment.ProcessPath` prilikom ponovnog pokretanja aplikacije nakon obnavljanja baze podataka. Time je sprečena greška `IL3000` pri Single-File objavljivanju i osigurano pouzdano pokretanje procesa.

## [1.0.55] - 2026-08-02

### 📋 Logovanje (`AppLog`, Serilog)
- **Uvedeno pravo logovanje u fajl.** Dijagnostika je do sada bila `Debug.WriteLine`, koji je u
  Release verziji potpuno nevidljiv — kada bi se kod korisnika nešto pokvarilo, nije ostajao trag.
- Zapisi idu u `%LOCALAPPDATA%\ERPiSredstvaApp\logs\log-GGGGMMDD.txt`, novi fajl svakog dana, čuva se
  poslednjih 14 dana. Zamenjuje raniji `crash.log` koji je rastao bez ograničenja.
- Postojeći globalni hvatači grešaka (korisnički interfejs, pozadinske niti, neposmatrani zadaci)
  premešteni u `AppLog` i sada pišu kroz logger umesto ručnog dopisivanja u fajl.
- `Debug.WriteLine` u `catch` blokovima prevedeni u `Serilog.Log.Error` — `BackupService`,
  `UserSettings`, `AppConfig`, `FirmePage`, `PodesavanjaPage`, `MainWindow`.

### 🐛 Ispravke
- **`AmortizacijaPage`** — spisak godina se gradio pozivom `.Value` nad `Godina` koja može biti prazna,
  što bi oborilo stranicu na prvom zapisu bez godine. Sada se takvi zapisi preskaču.

### 🛠️ Interno (bez uticaja na rad aplikacije)
- **CI kapija kvaliteta (`.github/workflows/release.yml`)**: workflow razdvojen na `test` i `build` job;
  release izlazi tek kada build i svih 38 testova prođu. Dodat `pull_request` triger tako da se izmena
  testira pre nego što uđe u granu. Ranije se nijedan test nije pokretao pre objavljivanja.
- **`Directory.Build.props`**: upozorenja prevodioca se u Release konfiguraciji tretiraju kao greške,
  pa nijedno ne može da prođe u objavljenu verziju. U Debug-u ostaju upozorenja.
- Uklonjen `TestDb` — pomoćni projekat koji nije bio u rešenju, nije se nigde referencirao i jedini je
  ciljao `net10.0` dok sve ostalo cilja `net8.0`.

---

## [1.0.54] - 2026-08-01

### 🎨 UI / UX
- Ikonica 🏢 na login ekranu sada bela (`Foreground="White"`) — ranije se renderovala crno i gubila na tamnom header-u.

---

## [1.0.52] - 2026-08-01

### 🚀 Nove funkcionalnosti i Sinhronizacija
- **Integracija sa ERPiFinansije (Poreska Amortizacija Obrazac OA)** — sinhronizovan proračun poreske amortizacije po I–V grupama sa Glavnom knjigom i Poreskim Bilansom (Obrazac PB-1).
- **Proširena evidencija fiksnih sredstava** — usklađena polja poreskih grupa, nabavnih i sadašnjih vrednosti za izvoz u PDF/Excel.

---

## [1.0.48] - 2026-07-30

### 🎨 Zvanična ERPi Ikonica
- **Novi Vizuelni Identitet**: Dodata nova visoko-rezoluciona ikona `app.ico` (motiv poslovne aktovke + ERPi SREDSTVA) na plavoj zaobljenoj podlozi (`#2563EB`).

---

## [1.0.47] - 2026-07-29

### 🎨 UI / UX i Odzivnost
- **Usklađene boje UI komponenti**: Vizuelne boje navigacije, dugmića i header elemenata usklađene sa zvaničnom paletom aplikacije (`PrimaryColor #1B4332`, `AccentColor #52B788`).
- **Osveženi prikazi svih stranica**: Usklađeni layout i stilovi za `MainWindow`, `DashboardPage`, `SredstvaPage`, `RashodPage`, `RevalorizacijaPage`, `PopisPage`, `DobavljaciPage`, `IzvestajiPage`, `KorisniciPage`, `FirmePage`, `PodesavanjaPage` i `PrijavaPage`.

---

## [1.0.45] - 2026-07-29


### 🚀 ERPiHub Integracija & CLI Ruting
- **Podrška za `--db-path` CLI parametar**: Omogućeno pokretanje `ERPiSredstvaApp.exe` iz ERPiHub centralnog kontrolnog panela sa automatskim prosleđivanjem putanje do baze podataka.

### 🎨 UI / UX Poboljšanja
- **Refaktorisana forma unosa prijave (`PrijavaWindow`)**: Proširena kolona za izbor Poreske grupe sa ToolTip prikazom zakonskih stopa, dodata unutrašnja margina na dugmad `+ Novi dobavljač` i `Dodaj` za bolji vizuelni odziv.

---

## [1.0.43] - 2026-07-24

### ✨ Nove Funkcionalnosti
- **Asistent za izbor Poreske grupe i stope (`PoreskaGrupaCatalog`)**:
  - Automatsko predlaganje zakonske Poreske grupe (I: 2.5%, II: 10%, III: 15%, IV: 20%, V: 30%) pri dodavanju novog sredstva na osnovu konta i naziva.
  - Integracija padajuće liste `PoreskaGrupa` u formu unosa nabavke (`PrijavaWindow`).
  - Dodata kolona `Poreska Grupa` u glavnoj tabeli osnovnih sredstava (`SredstvaPage`).

- **Izveštaj za Poreski Bilans (Obrazac PB-1 - PDF)**:
  - Generisanje zvaničnog PDF izveštaja sa obračunom privremenih poreskih razlika ($Računovodstvena - Poreska Amortizacija$).
  - Rekapitulacija sa ukupnim zbirnim iznosom spreman za direktan unos u Obrazac PB-1.

- **Čarobnjak za Masovnu Dodelu Poreskih Grupa**:
  - Dugme `🪄 Masovna Dodela Grupa` u tabu Poreske Amortizacije koje jednim klikom vrši masovnu analizu i dodelu zakonskih grupa i stopa za sva aktivna sredstva u bazi sa prikazom rekapitulacije po grupama.

- **Poreska Amortizacija za Stara Sredstva Pre 2019 (Čl. 4 & 7 Pravilnika)**:
  - Obračun degresivne amortizacije na ukupni saldo grupa II–V za sredstva nabavljena do 31.12.2018. godine.
  - Automatska primena **pravila 5 bruto plata u RS (mali saldo grupe)**: kada krajnji saldo grupe padne ispod 5 prosečnih bruto plata, celokupan saldo grupe se priznaje kao rashod amortizacije i saldo grupe postaje 0.

### ⚡ Poboljšanja i Ispravke
- **Automatska srazmerna amortizacija pri rashodovanju**: Prilikom rashodovanja / prodaje / otuđenja sredstva u toku godine, sistem automatski obračunava i knjiži srazmernu računovodstvenu amortizaciju od početka godine do datuma rashodovanja pre storniranja.
- **Mesečni i kvartalni obračun amortizacije**: Podrška za brze radne periode (`Godišnji`, `Q1`-`Q4`, `Proizvoljni period`) uz automatsko kreiranje opisnih dnevnika knjiženja.
- **Unapređena baza i migracije**: Automatska provera i unconditional patching SQLite tabela sa missing kolonama (`EnsureExtraColumnsExist`).

---

## [1.0.42] - 2026-07-24

### ✨ Nove Funkcionalnosti
- **MRS 16 Računovodstvena Amortizacija**:
  - Uvedena podrška za **Rezidualnu (spasavajuću) vrednost**: Osnovica za amortizaciju računa se kao $Nabavna - Rezidualna$ vrednost i sredstvo se ne otpisuje ispod rezidualnog iznosa.
  - Podrška za pravila početka amortizacije (`SrazmernoDanima` / `OdNarednogMeseca`).

- **Poreska Amortizacija (Obrazac OA)**:
  - Implementiran pojedinačni proporcionalni obračun poreske amortizacije za sredstva nabavljena od 1.1.2019. po grupama I–V.
  - Generisanje zvaničnog PDF izveštaja **Obrazac OA** preko QuestPDF biblioteke.

---

## [1.0.40] - 2026-07-20

### ✨ Nove Funkcionalnosti
- **Popisne liste i komisije**:
  - Definisanje popisnih komisija (Predsednik/Članovi).
  - Generisanje i štampa praznih popisnih listi za rad na terenu.
  - Obrada viškova i manjkova sa dinamičkim poljima za potpisivanje.
- **Nalepnice sa bar-kodovima**:
  - Štampanje CODE-128 bar-kod nalepnica u A4 formatu (3 kolone) za obeležavanje opreme uz masovnu selekciju sredstava.

---

## [1.0.0] - 2026-06-01

### 🚀 Inicijalno Izdanje
- Osnovna evidencija kartica osnovnih sredstava (nabavna, otpisana i sadašnja vrednost).
- Podrška za rad sa više firmi (multi-company SQLite arhitektura).
- Uvoz podataka iz starog Clipper/FoxPro DBF sistema.
- Prijava i upravljanje korisničkim nalozima (Administrator / Operater).
- Velopack integracija za automatsko ažuriranje aplikacije sa GitHub Releases.

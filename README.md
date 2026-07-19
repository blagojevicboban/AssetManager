# 🏢 SredstvaSystem — Evidencija Osnovnih Sredstava

> Desktop aplikacija za evidenciju osnovnih sredstava, amortizaciju, revalorizaciju i godišnje popise — razvijena u C# / .NET 8 / WPF.

---

## ✨ Funkcionalnosti

- 📊 **Radna tabla (Dashboard)** — vizuelni pregled statistike sredstava sa interaktivnim grafikonima.
- 📁 **Kartice (Sredstva)** — evidencija, kreiranje i praćenje osnovnih sredstava (nabavna i otpisana vrednost).
- 📉 **Amortizacija** — automatski godišnji obračun amortizacije, sa kreiranjem detaljnih PDF izveštaja (po kontu i obračunskim jedinicama).
- 📈 **Revalorizacija** — obračun revalorizacije po definisanim koeficijentima i ažuriranje sadašnje vrednosti sredstava.
- 📋 **Popisne liste** — kreiranje popisnih komisija sa definisanjem članova i uloga (Predsednik/Član), štampanje praznih listi za terenski rad i obrada knjigovodstvenih odstupanja i viškova/manjkova kroz integrisani UI. PDF izveštaji generišu dinamička polja sa imenima članova za potpisivanje.
- 🖨 **Nalepnice sa kodovima** — štampanje bar-kod (CODE-128) nalepnica za obeležavanje opreme, sa podrškom za vizuelnu selekciju više sredstava i automatskim generisanjem prilagođenog PDF rasporeda za A4 format nalepnica koristeći ZXing.Net.
- 📄 **Štampa i izveštaji** — izveštaji se generišu u PDF formatu preko **QuestPDF** biblioteke i prilagođeni su za A3/A4 landscape format štampe.

---

## 🛠️ Tehnologije

| Oblast | Tehnologija |
|---|---|
| Jezik | C# 12 / .NET 8.0 |
| UI | WPF (Windows Presentation Foundation) |
| Grafikoni | LiveCharts2 (SkiaSharp) |
| Arhitektura | Code-Behind (bez striktnog MVVM-a radi brzine razvoja) |
| Baza podataka | SQLite |
| ORM | Entity Framework Core 8 |
| Izveštaji / PDF | QuestPDF |
| Pakovanje / Update | Velopack |
| CI/CD | GitHub Actions |

---

## 📁 Struktura projekta

```
SredstvaSystem/
├── SredstvaApp/            # Glavni WPF projekat (Stranice, PDF Dokumenti)
│   ├── Views/              # Podmoduli (Amortizacija, Revalorizacija, Popis, Kartice)
│   └── Resources/          # Stilovi, Uputstvo (Help dokumentacija)
├── SredstvaData/           # Data Access Layer (EF Core entiteti, DbContext)
│   └── Models/             # Sredstvo, Kartica, Prijava, Rashod, Popis...
├── SredstvaMigration/      # Alat za migraciju legacy podataka iz starih DBF Clipper fajlova
├── .github/workflows/      # GitHub Actions za CI/CD i automatski release
└── version.txt             # Fajl iz koga skripta cita trenutnu verziju za auto-update
```

---

## 🚀 Pokretanje projekta (za razvoj)

### Preduslovi
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8)
- Visual Studio 2022+ ili JetBrains Rider

### Koraci
```bash
# 1. Klonirati repozitorijum (ako ste ga postavili na Git)
git clone https://github.com/vas-profil/SredstvaSystem.git
cd SredstvaSystem

# 2. Prevesti projekat
dotnet build

# 3. Pokrenuti aplikaciju
dotnet run --project SredstvaApp/SredstvaApp.csproj
```
> **Napomena:** Lokalna SQLite baza (`sredstva.db`) automatski se kreira na prvoj instanci ukoliko već ne postoji. Ukoliko imate DBF podatke, prvo pokrenite projekat `SredstvaMigration`.

---

## 📦 Instalacija (za krajnje korisnike)
Kada je CI/CD aktivan, preuzmite najnoviji `SredstvaAppSetup.exe` sa GitHub Releases stranice.
Aplikacija se instalira u profil korisnika bez administratorskih prava, a svako novo ažuriranje (Nova verzija u `version.txt`) biće primenjeno automatski kroz **Velopack Delta Update**.

---

## 🔒 Napomene o bazi podataka
- Lokalni fajl `sredstva.db` sa podacima preduzeća je **isključen iz Git repozitorijuma** i ostaje isključivo na lokalnoj mašini korisnika.

---
*Aplikacija služi za prelaz sa nasleđenog Clipper MS-DOS softvera i u potpunosti replikuje logiku iz originalnih PRG modula (AMORTIZ.PRG, REVALOR.PRG, POPIS.PRG).*

<br/>
<br/>

# 🏢 SredstvaSystem — Fixed Assets Management (English)

> Desktop application for fixed assets management, depreciation, revaluation, and annual inventory — developed in C# / .NET 8 / WPF.

---

## ✨ Features

- 📊 **Dashboard** — visual overview of asset statistics with interactive charts.
- 📁 **Assets (Cards)** — registration, creation, and tracking of fixed assets (purchase and written-off value).
- 📉 **Depreciation** — automatic annual calculation of depreciation, with the creation of detailed PDF reports (by account and accounting units).
- 📈 **Revaluation** — revaluation calculation according to defined coefficients and updating the present value of assets.
- 📋 **Inventory Lists** — creation of inventory commissions with defined members and roles (President/Member), printing of empty lists for field work, and processing accounting deviations and surpluses/shortages through an integrated UI. PDF reports feature dynamic signature fields with member names.
- 📄 **Printing and Reports** — reports are generated in PDF format via the **QuestPDF** library and are adapted for A3/A4 landscape printing format.

---

## 🛠️ Technologies

| Area | Technology |
|---|---|
| Language | C# 12 / .NET 8.0 |
| UI | WPF (Windows Presentation Foundation) |
| Charts | LiveCharts2 (SkiaSharp) |
| Architecture | Code-Behind (without strict MVVM for development speed) |
| Database | SQLite |
| ORM | Entity Framework Core 8 |
| Reports / PDF | QuestPDF |
| Packaging / Update | Velopack |
| CI/CD | GitHub Actions |

---

## 📁 Project Structure

```
SredstvaSystem/
├── SredstvaApp/            # Main WPF project (Pages, PDF Documents)
│   ├── Views/              # Submodules (Depreciation, Revaluation, Inventory, Cards)
│   └── Resources/          # Styles, Manual (Help documentation)
├── SredstvaData/           # Data Access Layer (EF Core entities, DbContext)
│   └── Models/             # Asset, Card, Registration, Write-off, Inventory...
├── SredstvaMigration/      # Migration tool for legacy data from old DBF Clipper files
├── .github/workflows/      # GitHub Actions for CI/CD and automatic release
└── version.txt             # File from which the script reads the current version for auto-update
```

---

## 🚀 Running the Project (for development)

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8)
- Visual Studio 2022+ or JetBrains Rider

### Steps
```bash
# 1. Clone the repository
git clone https://github.com/blagojevicboban/AssetManager.git
cd AssetManager

# 2. Build the project
dotnet build

# 3. Run the application
dotnet run --project SredstvaApp/SredstvaApp.csproj
```
> **Note:** The local SQLite database (`sredstva.db`) is automatically created on the first instance if it doesn't already exist. If you have DBF data, first run the `SredstvaMigration` project.

---

## 📦 Installation (for end users)
When CI/CD is active, download the latest `SredstvaAppSetup.exe` from the GitHub Releases page.
The application is installed in the user's profile without administrator privileges, and every new update (New version in `version.txt`) will be applied automatically through **Velopack Delta Update**.

---

## 🔒 Database Notes
- The local `sredstva.db` file containing company data is **excluded from the Git repository** and remains exclusively on the user's local machine.

---
*The application serves for the transition from the legacy Clipper MS-DOS software and fully replicates the logic from the original PRG modules.*

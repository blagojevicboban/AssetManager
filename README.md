# 🏢 SredstvaSystem — Evidencija Osnovnih Sredstava

> Desktop aplikacija za evidenciju osnovnih sredstava, amortizaciju, revalorizaciju i godišnje popise — razvijena u C# / .NET 8 / WPF.

**Autor:** Blagojević Boban

---

## ✨ Funkcionalnosti

- 🔐 **Prijava i korisnici** — pristup aplikaciji preko korisničkog naloga (lozinka + uloga), sa modulom "Korisnici" za kreiranje naloga, dodelu uloga (Administrator / Operater), poništavanje lozinke i deaktivaciju.
- 🏢 **Firme (rad sa više preduzeća)** — evidencija proizvoljnog broja firmi u istoj instalaciji, svaka sa sopstvenom SQLite bazom podataka, uz brzo prebacivanje koja je firma trenutno aktivna za rad i izveštaje.
- 📊 **Radna tabla (Dashboard)** — vizuelni pregled statistike sredstava sa interaktivnim grafikonima.
- 🏗️ **Osnovna sredstva (Kartice)** — evidencija, kreiranje i praćenje osnovnih sredstava (nabavna, otpisana i sadašnja vrednost), sa masovnom selekcijom (uključujući "izaberi sve") za akcije poput štampe nalepnica.
- 📋 **Analitičke kartice** — istorijski pregled svih promena (nabavka, amortizacija, revalorizacija, rashod) za pojedinačno sredstvo.
- 🏢 **Dobavljači** — šifarnik dobavljača (po kontu) sa pregledom svih prijava sredstava vezanih za odabranog dobavljača.
- 📥 **Prijava sredstava** — unos naloga za nabavku/aktiviranje novih sredstava, sa pregledom naloga i stavki (master-detail), i mogućnošću izmene proknjiženih i neproknjiženih naloga.
- 📤 **Rashod i promene** — evidencija rashodovanja, prodaje, otuđenja, prenosa u drugu obračunsku jedinicu, brisanja i povećanja vrednosti/količine/amortizacije kroz naloge, sa pregledom naloga i stavki (master-detail) i PDF štampom naloga.
- 📉 **Amortizacija** — automatski godišnji obračun amortizacije, sa kreiranjem detaljnih PDF izveštaja (po kontu i obračunskim jedinicama).
- 📈 **Revalorizacija** — obračun revalorizacije po definisanim koeficijentima i ažuriranje sadašnje vrednosti sredstava.
- 📋 **Popisne liste** — kreiranje popisnih komisija sa definisanjem članova i uloga (Predsednik/Član), štampanje praznih listi za terenski rad i obrada knjigovodstvenih odstupanja i viškova/manjkova kroz integrisani UI. PDF izveštaji generišu dinamička polja sa imenima članova za potpisivanje.
- 📑 **Izveštaji i Rekapitulacije** — popis svih sredstava i rekapitulacije grupisane po kontu, obračunskoj jedinici ili amortizacionoj grupi, sa izvozom u CSV.
- 🖨 **Nalepnice sa kodovima** — štampanje bar-kod (CODE-128) nalepnica za obeležavanje opreme, sa podrškom za vizuelnu selekciju više sredstava i automatskim generisanjem prilagođenog PDF rasporeda za A4 format nalepnica koristeći ZXing.Net.
- ⚙️ **Podešavanja** — ručna i automatska rezervna kopija baze (backup/restore) sa istorijom kopija, uvoz podataka iz starog DOS/FoxPro programa (DBF fajlovi), i opšte postavke ponašanja programa.
- 📄 **Štampa i izveštaji** — izveštaji se generišu u PDF formatu preko **QuestPDF** biblioteke i prilagođeni su za A3/A4 landscape format štampe.

---

## 🛠️ Tehnologije

| Oblast | Tehnologija |
| --- | --- |
| Jezik | C# 12 / .NET 8.0 |
| UI | WPF (Windows Presentation Foundation) |
| Grafikoni | LiveCharts2 (SkiaSharp) |
| Arhitektura | Code-Behind (bez striktnog MVVM-a radi brzine razvoja) |
| Baza podataka | SQLite (po jedna baza po firmi) |
| ORM | Entity Framework Core 8 |
| Izveštaji / PDF | QuestPDF |
| Bar-kodovi | ZXing.Net |
| Pakovanje / Update | Velopack |
| CI/CD | GitHub Actions |

---

## 📁 Struktura projekta

```text
SredstvaSystem/
├── SredstvaApp/            # Glavni WPF projekat (Stranice, PDF Dokumenti)
│   ├── Views/
│   │   ├── Korisnici/      # Prijava (Login) i upravljanje korisničkim nalozima
│   │   ├── Firme/          # Rad sa više firmi / preduzeća
│   │   ├── Dashboard/      # Radna tabla sa grafikonima
│   │   ├── Sredstva/       # Osnovna sredstva (kartice, nalepnice)
│   │   ├── Kartice/        # Analitičke kartice pojedinačnog sredstva
│   │   ├── Dobavljaci/     # Šifarnik dobavljača
│   │   ├── Prijave/        # Prijava (nabavka) sredstava
│   │   ├── Rashod/         # Rashod i promene sredstava
│   │   ├── Amortizacija/   # Godišnji obračun amortizacije
│   │   ├── Revalorizacija/ # Obračun revalorizacije
│   │   ├── Popis/          # Popisne komisije i popisne liste
│   │   ├── Izvestaji/      # Izveštaji i rekapitulacije
│   │   └── Podesavanja/    # Backup/restore, uvoz iz starog programa, postavke
│   └── Resources/          # Stilovi, Uputstvo (Help dokumentacija)
├── SredstvaData/           # Data Access Layer (EF Core entiteti, DbContext)
│   └── Models/             # Sredstvo, Kartica, Prijava, Rashod, Popis, Dobavljac, Firma, Korisnik...
├── SredstvaData.Tests/     # Unit testovi (npr. obračun amortizacije)
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

> **Napomena:** Lokalna SQLite baza (`sredstva.db`) automatski se kreira na prvoj instanci ukoliko već ne postoji. Ukoliko imate DBF podatke, prvo pokrenite projekat `SredstvaMigration` ili iskoristite uvoz kroz tab "Uvoz iz starog programa" u Podešavanjima.

### Testovi

```bash
dotnet test SredstvaData.Tests/SredstvaData.Tests.csproj
```

---

## 📦 Instalacija (za krajnje korisnike)

Kada je CI/CD aktivan, preuzmite najnoviji `SredstvaAppSetup.exe` sa GitHub Releases stranice.
Aplikacija se instalira u profil korisnika bez administratorskih prava, a svako novo ažuriranje (Nova verzija u `version.txt`) biće primenjeno automatski kroz **Velopack Delta Update**.

---

## 🔒 Napomene o bazi podataka

- Lokalni fajl(ovi) `*.db` sa podacima preduzeća su **isključeni iz Git repozitorijuma** i ostaju isključivo na lokalnoj mašini korisnika.
- Svaka firma dodata kroz modul "Firme" ima svoju sopstvenu bazu podataka; aktivna firma se bira iz istog modula.
- Redovna rezervna kopija (ručna ili automatska) se pravi kroz "Podešavanja → Rezervna kopija".

---
*Aplikacija služi za prelaz sa nasleđenog Clipper MS-DOS softvera i u potpunosti replikuje logiku iz originalnih PRG modula (AMORTIZ.PRG, REVALOR.PRG, POPIS.PRG).*

© 2026 Blagojević Boban. Sva prava zadržana.

<br/>
<br/>

# 🏢 SredstvaSystem — Fixed Assets Management (English)

> Desktop application for fixed assets management, depreciation, revaluation, and annual inventory — developed in C# / .NET 8 / WPF.

**Author:** Blagojević Boban

---

## ✨ Features

- 🔐 **Login and users** — access to the application via a user account (password + role), with a "Korisnici" (Users) module for creating accounts, assigning roles (Administrator / Operator), resetting passwords, and deactivating accounts.
- 🏢 **Companies (multi-company support)** — record any number of companies in the same installation, each with its own SQLite database, with quick switching of which company is currently active for work and reports.
- 📊 **Dashboard** — visual overview of asset statistics with interactive charts.
- 🏗️ **Fixed Assets (Cards)** — registration, creation, and tracking of fixed assets (purchase, written-off, and present value), with bulk selection (including "select all") for actions such as label printing.
- 📋 **Analytical Cards** — historical view of all changes (acquisition, depreciation, revaluation, write-off) for an individual asset.
- 🏢 **Suppliers (Dobavljači)** — a supplier registry (by account) with an overview of all asset registrations linked to the selected supplier.
- 📥 **Asset Registration (Prijava)** — enter orders for acquiring/activating new assets, with an order + line-item overview (master-detail), and the ability to edit both posted and unposted orders.
- 📤 **Write-offs and Changes (Rashod)** — record write-offs, sales, disposals, transfers to another accounting unit, deletions, and increases in value/quantity/depreciation through orders, with an order + line-item overview (master-detail) and PDF printing of orders.
- 📉 **Depreciation** — automatic annual calculation of depreciation, with the creation of detailed PDF reports (by account and accounting units).
- 📈 **Revaluation** — revaluation calculation according to defined coefficients and updating the present value of assets.
- 📋 **Inventory Lists** — creation of inventory commissions with defined members and roles (President/Member), printing of empty lists for field work, and processing accounting deviations and surpluses/shortages through an integrated UI. PDF reports feature dynamic signature fields with member names.
- 📑 **Reports and Recapitulations** — a full asset listing plus recapitulations grouped by account, accounting unit, or depreciation group, with CSV export.
- 🖨 **Barcode Labels** — printing of barcode (CODE-128) labels for marking equipment, with support for visually selecting multiple assets and automatic generation of a custom PDF layout for A4-format labels using ZXing.Net.
- ⚙️ **Settings** — manual and automatic database backup/restore with backup history, importing data from the old DOS/FoxPro program (DBF files), and general application behavior settings.
- 📄 **Printing and Reports** — reports are generated in PDF format via the **QuestPDF** library and are adapted for A3/A4 landscape printing format.

---

## 🛠️ Technologies

| Area | Technology |
| --- | --- |
| Language | C# 12 / .NET 8.0 |
| UI | WPF (Windows Presentation Foundation) |
| Charts | LiveCharts2 (SkiaSharp) |
| Architecture | Code-Behind (without strict MVVM for development speed) |
| Database | SQLite (one database per company) |
| ORM | Entity Framework Core 8 |
| Reports / PDF | QuestPDF |
| Barcodes | ZXing.Net |
| Packaging / Update | Velopack |
| CI/CD | GitHub Actions |

---

## 📁 Project Structure

```text
SredstvaSystem/
├── SredstvaApp/            # Main WPF project (Pages, PDF Documents)
│   ├── Views/
│   │   ├── Korisnici/      # Login and user account management
│   │   ├── Firme/          # Multi-company management
│   │   ├── Dashboard/      # Dashboard with charts
│   │   ├── Sredstva/       # Fixed assets (cards, labels)
│   │   ├── Kartice/        # Analytical cards for individual assets
│   │   ├── Dobavljaci/     # Supplier registry
│   │   ├── Prijave/        # Asset registration (acquisition)
│   │   ├── Rashod/         # Asset write-offs and changes
│   │   ├── Amortizacija/   # Annual depreciation calculation
│   │   ├── Revalorizacija/ # Revaluation calculation
│   │   ├── Popis/          # Inventory commissions and inventory lists
│   │   ├── Izvestaji/      # Reports and recapitulations
│   │   └── Podesavanja/    # Backup/restore, legacy import, settings
│   └── Resources/          # Styles, Manual (Help documentation)
├── SredstvaData/           # Data Access Layer (EF Core entities, DbContext)
│   └── Models/             # Asset, Card, Registration, Write-off, Inventory, Supplier, Company, User...
├── SredstvaData.Tests/     # Unit tests (e.g. depreciation calculation)
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

> **Note:** The local SQLite database (`sredstva.db`) is automatically created on the first instance if it doesn't already exist. If you have DBF data, first run the `SredstvaMigration` project, or use the import feature under the "Uvoz iz starog programa" tab in Settings.

### Tests

```bash
dotnet test SredstvaData.Tests/SredstvaData.Tests.csproj
```

---

## 📦 Installation (for end users)

When CI/CD is active, download the latest `SredstvaAppSetup.exe` from the GitHub Releases page.
The application is installed in the user's profile without administrator privileges, and every new update (New version in `version.txt`) will be applied automatically through **Velopack Delta Update**.

---

## 🔒 Database Notes

- The local `*.db` file(s) containing company data are **excluded from the Git repository** and remain exclusively on the user's local machine.
- Each company added through the "Firme" (Companies) module has its own database; the active company is selected from the same module.
- Regular backups (manual or automatic) are created via "Settings → Backup".

---
*The application serves for the transition from the legacy Clipper MS-DOS software and fully replicates the logic from the original PRG modules.*

© 2026 Blagojević Boban. All rights reserved.

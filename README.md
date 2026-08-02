# 🏢 ERPiSredstva — Evidencija Osnovnih Sredstava

> Desktop aplikacija za evidenciju osnovnih sredstava, amortizaciju, revalorizaciju i godišnje popise — razvijena u C# / .NET 8 / WPF.

**Autor:** Blagojević Boban

---

## ✨ Funkcionalnosti

- 🔐 **Prijava i korisnici** — pristup aplikaciji preko korisničkog naloga (lozinka + uloga), sa modulom "Korisnici" za kreiranje naloga, dodelu uloga (Administrator / Operater), poništavanje lozinke i deaktivaciju.
- 🏢 **Firme (rad sa više preduzeća)** — evidencija proizvoljnog broja firmi u istoj instalaciji, svaka sa sopstvenom SQLite bazom podataka, uz brzo prebacivanje koja je firma trenutno aktivna za rad i izveštaje.
- 📊 **Radna tabla (Dashboard)** — vizuelni pregled statistike sredstava sa interaktivnim grafikonima.
- 🏗️ **Osnovna sredstva (Kartice)** — evidencija, kreiranje i praćenje osnovnih sredstava (nabavna, rezidualna, otpisana i sadašnja vrednost po MRS 16), sa masovnom selekcijom za akcije poput štampe nalepnica.
- 📋 **Analitičke kartice** — istorijski pregled svih promena (nabavka, amortizacija, revalorizacija, rashod) za pojedinačno sredstvo.
- 🏢 **Dobavljači** — šifarnik dobavljača (po kontu) sa pregledom svih prijava sredstava vezanih za odabranog dobavljača.
- 📥 **Prijava sredstava** — unos naloga za nabavku/aktiviranje novih sredstava, sa automatskim asistentom za izbor zakonske Poreske grupe i stope (`PoreskaGrupaCatalog`), i pregledom naloga i stavki (master-detail).
- 📤 **Rashod i promene** — evidencija rashodovanja, prodaje, otuđenja, prenosa u drugu obračunsku jedinicu i povećanja vrednosti, uz **automatski srazmeran obračun računovodstvene amortizacije do datuma rashodovanja**.
- 📉 **Amortizacija (MRS 16 & Poreska)** — 
  - **Računovodstvena amortizacija (MRS 16)**: Podrška za rezidualnu vrednost, pravila početka amortizacije (`SrazmernoDanima` / `OdNarednogMeseca`), kao i mesečni, kvartalni i proizvoljni periodični obračun.
  - **Poreska amortizacija (Obrazac OA)**: Obračun pojedinačne poreske amortizacije za sredstva nabavljena od 1.1.2019. po zakonskim grupama I–V, uz generisanje PDF **Obrasca OA**.
  - **Poreski Bilans (Obrazac PB-1)**: Obračun privremenih poreskih razlika (Računovodstvena − Poreska amortizacija) sa izvozom zvaničnog PDF izveštaja za Poreski Bilans.
  - **Čarobnjak za masovnu dodelu**: Automatska dodela poreskih grupa i stopa postojećim sredstvima u bazi.
  - **Stara sredstva pre 2019 (Čl. 4 & 7 Pravilnika)**: Obračun degresivnog salda grupa II–V i automatska primena pravila 5 bruto zarada (mali saldo grupe).
- 📈 **Revalorizacija** — obračun revalorizacije po definisanim koeficijentima i ažuriranje sadašnje vrednosti sredstava.
- 📋 **Popisne liste** — kreiranje popisnih komisija sa definisanjem članova i uloga (Predsednik/Član), štampanje praznih listi za terenski rad i obrada knjigovodstvenih odstupanja i viškova/manjkova kroz integrisani UI. PDF izveštaji generišu dinamička polja sa imenima članova za potpisivanje.
- 📑 **Izveštaji i Rekapitulacije** — popis svih sredstava i rekapitulacije grupisane po kontu, obračunskoj jedinici ili amortizacionoj grupi, sa izvozom u CSV.
- 🖨 **Nalepnice sa kodovima** — štampanje bar-kod (CODE-128) nalepnica za obeležavanje opreme, sa podrškom za vizuelnu selekciju više sredstava i automatskim generisanjem prilagođenog PDF rasporeda za A4 format nalepnica koristeći ZXing.Net.
- ⚙️ **Podešavanja** — ručna i automatska rezervna kopija baze (backup/restore) sa istorijom kopija, uvoz podataka iz starog DOS/FoxPro programa (DBF fajlovi), i opšte postavke ponašanja programa.
- 📄 **Štampa i izveštaji** — izveštaji se generišu u PDF formatu preko **QuestPDF** biblioteke i prilagođeni su za A3/A4 landscape/portrait format štampe.

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
ERPiSredstva/
├── ERPiSredstvaApp/            # Glavni WPF projekat (Stranice, PDF Dokumenti)
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
├── ERPiSredstvaData/           # Data Access Layer (EF Core entiteti, DbContext)
│   └── Models/             # Sredstvo, Kartica, Prijava, Rashod, Popis, Dobavljac, Firma, Korisnik...
├── ERPiSredstvaData.Tests/     # Unit testovi (npr. obračun amortizacije)
├── ERPiSredstvaMigration/      # Alat za migraciju legacy podataka iz starih DBF Clipper fajlova
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
git clone https://github.com/vas-profil/ERPiSredstva.git
cd ERPiSredstva

# 2. Prevesti projekat
dotnet build

# 3. Pokrenuti aplikaciju
dotnet run --project ERPiSredstvaApp/ERPiSredstvaApp.csproj
```

> **Napomena:** Lokalna SQLite baza (`sredstva.db`) automatski se kreira na prvoj instanci ukoliko već ne postoji. Ukoliko imate DBF podatke, prvo pokrenite projekat `ERPiSredstvaMigration` ili iskoristite uvoz kroz tab "Uvoz iz starog programa" u Podešavanjima.

### Testovi

```bash
dotnet test ERPiSredstvaData.Tests/ERPiSredstvaData.Tests.csproj
```

---

## 📦 Instalacija (za krajnje korisnike)

Kada je CI/CD aktivan, preuzmite najnoviji `ERPiSredstvaAppSetup.exe` sa GitHub Releases stranice.
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

# 🏢 ERPiSredstva — Fixed Assets Management (English)

> Desktop application for fixed assets management, depreciation, revaluation, and annual inventory — developed in C# / .NET 8 / WPF.

**Author:** Blagojević Boban

---

## ✨ Features

- 🔐 **Login and users** — access to the application via a user account (password + role), with a "Korisnici" (Users) module for creating accounts, assigning roles (Administrator / Operator), resetting passwords, and deactivating accounts.
- 🏢 **Companies (multi-company support)** — record any number of companies in the same installation, each with its own SQLite database, with quick switching of which company is currently active for work and reports.
- 📊 **Dashboard** — visual overview of asset statistics with interactive charts.
- 🏗️ **Fixed Assets (Cards)** — registration, creation, and tracking of fixed assets (purchase, residual, written-off, and present value per IAS 16), with bulk selection for actions such as label printing.
- 📋 **Analytical Cards** — historical view of all changes (acquisition, depreciation, revaluation, write-off) for an individual asset.
- 🏢 **Suppliers (Dobavljači)** — a supplier registry (by account) with an overview of all asset registrations linked to the selected supplier.
- 📥 **Asset Registration (Prijava)** — enter orders for acquiring/activating new assets, with automatic smart assistant for statutory Tax Group & rate recommendation (`PoreskaGrupaCatalog`), and an order + line-item overview (master-detail).
- 📤 **Write-offs and Changes (Rashod)** — record write-offs, sales, disposals, transfers to another accounting unit, and increases in value, with **automatic proportional pre-disposal accounting depreciation calculation**.
- 📉 **Depreciation (IAS 16 & Tax Depreciation)** — 
  - **Accounting Depreciation (IAS 16)**: Residual value support, depreciation start rules (`ProportionalDays` / `NextMonth`), and monthly, quarterly, or custom period calculations.
  - **Tax Depreciation (Form OA)**: Individual tax depreciation calculation for assets acquired since Jan 1, 2019 by statutory tax groups I–V, with PDF **Form OA** generation.
  - **Tax Balance (Form PB-1)**: Temporary tax differences calculation (Accounting − Tax Depreciation) with export of official PDF reports for Form PB-1.
  - **Bulk Assignment Wizard**: Automatic batch assignment of tax groups and rates to existing assets in the database.
  - **Legacy Pre-2019 Assets (Art. 4 & 7 Rulebook)**: Collective declining-balance group II–V calculation and automatic 5 gross salary small balance threshold rule.
- 📈 **Revaluation** — revaluation calculation according to defined coefficients and updating the present value of assets.
- 📋 **Inventory Lists** — creation of inventory commissions with defined members and roles (President/Member), printing of empty lists for field work, and processing accounting deviations and surpluses/shortages through an integrated UI. PDF reports feature dynamic signature fields with member names.
- 📑 **Reports and Recapitulations** — a full asset listing plus recapitulations grouped by account, accounting unit, or depreciation group, with CSV export.
- 🖨 **Barcode Labels** — printing of barcode (CODE-128) labels for marking equipment, with support for visually selecting multiple assets and automatic generation of a custom PDF layout for A4-format labels using ZXing.Net.
- ⚙️ **Settings** — manual and automatic database backup/restore with backup history, importing data from the old DOS/FoxPro program (DBF files), and general application behavior settings.
- 📄 **Printing and Reports** — reports are generated in PDF format via the **QuestPDF** library and are adapted for A3/A4 landscape/portrait printing format.

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
ERPiSredstva/
├── ERPiSredstvaApp/            # Main WPF project (Pages, PDF Documents)
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
├── ERPiSredstvaData/           # Data Access Layer (EF Core entities, DbContext)
│   └── Models/             # Asset, Card, Registration, Write-off, Inventory, Supplier, Company, User...
├── ERPiSredstvaData.Tests/     # Unit tests (e.g. depreciation calculation)
├── ERPiSredstvaMigration/      # Migration tool for legacy data from old DBF Clipper files
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
git clone https://github.com/blagojevicboban/ERPiSredstva.git
cd ERPiSredstva

# 2. Build the project
dotnet build

# 3. Run the application
dotnet run --project ERPiSredstvaApp/ERPiSredstvaApp.csproj
```

> **Note:** The local SQLite database (`sredstva.db`) is automatically created on the first instance if it doesn't already exist. If you have DBF data, first run the `ERPiSredstvaMigration` project, or use the import feature under the "Uvoz iz starog programa" tab in Settings.

### Tests

```bash
dotnet test ERPiSredstvaData.Tests/ERPiSredstvaData.Tests.csproj
```

---

## 📦 Installation (for end users)

When CI/CD is active, download the latest `ERPiSredstvaAppSetup.exe` from the GitHub Releases page.
The application is installed in the user's profile without administrator privileges, and every new update (New version in `version.txt`) will be applied automatically through **Velopack Delta Update**.

---

## 🔒 Database Notes

- The local `*.db` file(s) containing company data are **excluded from the Git repository** and remain exclusively on the user's local machine.
- Each company added through the "Firme" (Companies) module has its own database; the active company is selected from the same module.
- Regular backups (manual or automatic) are created via "Settings → Backup".

---
*The application serves for the transition from the legacy Clipper MS-DOS software and fully replicates the logic from the original PRG modules.*

© 2026 Blagojević Boban. All rights reserved.

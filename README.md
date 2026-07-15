# 🏢 SredstvaSystem — Evidencija Osnovnih Sredstava

> Desktop aplikacija za evidenciju osnovnih sredstava, amortizaciju, revalorizaciju i godišnje popise — razvijena u C# / .NET 8 / WPF.

---

## ✨ Funkcionalnosti

- 📁 **Kartice (Sredstva)** — evidencija, kreiranje i praćenje osnovnih sredstava (nabavna i otpisana vrednost).
- 📉 **Amortizacija** — automatski godišnji obračun amortizacije, sa kreiranjem detaljnih PDF izveštaja (po kontu i obračunskim jedinicama).
- 📈 **Revalorizacija** — obračun revalorizacije po definisanim koeficijentima i ažuriranje sadašnje vrednosti sredstava.
- 📋 **Popisne liste** — kreiranje popisnih komisija, štampanje praznih listi za terenski rad i obrada knjigovodstvenih odstupanja i viškova/manjkova kroz integrisani UI.
- 📄 **Štampa i izveštaji** — izveštaji se generišu u PDF formatu preko **QuestPDF** biblioteke i prilagođeni su za A3/A4 landscape format štampe.

---

## 🛠️ Tehnologije

| Oblast | Tehnologija |
|---|---|
| Jezik | C# 12 / .NET 8.0 |
| UI | WPF (Windows Presentation Foundation) |
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

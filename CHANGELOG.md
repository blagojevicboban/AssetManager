# 📋 Istorija izmena (Changelog) — SredstvaSystem

Sve značajne promene i novine u aplikaciji **SredstvaSystem** dokumentovane su u ovom fajlu.

Format je zasnovan na [Keep a Changelog](https://keepachangelog.com/en/1.0.0/) standardu i prati Semantic Versioning.

---

## [1.0.54] - 2026-08-01

### 🎨 UI / UX
- Ikonica 🏢 na login ekranu sada bela (`Foreground="White"`) — ranije se renderovala crno i gubila na tamnom header-u.

---

## [1.0.52] - 2026-08-01

### 🚀 Nove funkcionalnosti i Sinhronizacija
- **Integracija sa AccountingSystem (Poreska Amortizacija Obrazac OA)** — sinhronizovan proračun poreske amortizacije po I–V grupama sa Glavnom knjigom i Poreskim Bilansom (Obrazac PB-1).
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


### 🚀 ErpHub Integracija & CLI Ruting
- **Podrška za `--db-path` CLI parametar**: Omogućeno pokretanje `SredstvaApp.exe` iz ErpHub centralnog kontrolnog panela sa automatskim prosleđivanjem putanje do baze podataka.

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

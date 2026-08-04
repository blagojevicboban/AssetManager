# ERPiSredstva — Analiza legacy Clipper sistema i plan razvoja

> Nastalo iz analize `SREDSTVA.CLP` i pratećih `.PRG` modula u
> `C:\FIRME\ARHIBEL\SREDSTVA` (klijent "Arhibel", korisnik #25) i strukture
> `.DBF` baza u `C:\FIRME\ARHIBEL\SREDSTVA\Kor25`. Isti obrazac dokumenta kao
> `ERPiFinansije/ANALIZA_I_PLAN.md`, ali za modul osnovnih sredstava.
> Verzija dokumenta: 2026-08-04.

**Za razliku od ERPiFinansije** (rewrite u toku, dokument tamo prati fazni
napredak), **ERPiSredstva je već završen rewrite** — README i CHANGELOG
pokazuju da je svaka stavka glavnog menija DOS programa portovana, uz niz
mogućnosti koje stari program nikad nije imao (vidi §5). Ovaj dokument je
zato pre svega **referenca** (šta je stari program radio, kako se mapira na
nove modele) i **spisak namernih odstupanja/otvorenih pitanja**, a ne fazni
plan izgradnje od nule.

---

## 1. Šta je legacy sistem (DOS / Clipper)

Za razliku od `ERPiFinansije` (4 modula: FIN/ANAL/ROB/MAT), **osnovna
sredstva su jedan samostalan Clipper program** — `SREDSTVA.CLP` — koji radi
nad istim obrascem organizacije kao i knjigovodstvo: **jedan direktorijum po
firmi** (`KORxx`, ovde `Kor25` za Arhibel), a `KORISNIC.DBF` u korenu drži
podatke o svim firmama koje program zna.

`GLAVNI.PRG` je launcher (isti naziv i uloga kao u FIN-u): učitava firmu,
prijavljuje korisnika (lozinka + evidencija korisnika, funkcije
`novikorisnik`/`izmenakorisnika`/`delkorisnika`) i grana na glavni meni.
Firma se, kao i u knjigovodstvu, bira **hardkodovanjem konstante**
(`korisnik:=25 // Arhibel`) — u fajlu postoji zakomentarisana lista svih
ranijih klijenata (1–20) za koje je isti izvorni kod prekompajliran.

### 1.1 Glavni meni (`glmeni`, GLAVNI.PRG:508)

| Stavka menija | .PRG | Namena |
| --- | --- | --- |
| Šifrarnik dobavljača | `GLAVNI.PRG` (`dobavljaci`) | CRUD nad `KONTPLAN.DBF` — u ovom modulu to je šifarnik dobavljača "po kontu", ne kontni plan |
| Prijava sredstava | `PRIJAVA.PRG` | Unos naloga za nabavku/aktiviranje |
| Rashod-prodaja-otuđenje | `RASHOD.PRG` | Sve promene koje smanjuju/menjaju stanje sredstva |
| Amortizacija | `AMORTIZ.PRG` (i identičan `AMORTIZs.PRG`) | Obračun, štampa, knjiženje, promena stopa |
| Revalorizacija | `REVALOR.PRG` | Obračun, štampa, knjiženje |
| Kartice | `KARTICE.PRG` | Pregled/štampa analitičke kartice sredstva |
| Popisne liste | `POPIS.PRG` | **Samo štampa** — vidi §4 |

`AMORTIZs.PRG` je bajt-za-bajt identičan `AMORTIZ.PRG` (verovatno stara
kopija ostavljena kao rezerva) — nema samostalnu funkciju u sistemu.

### 1.2 Podmeniji

| Modul | Podmeni (funkcija) | Stavke |
| --- | --- | --- |
| Amortizacija | `meni5`, AMORTIZ.PRG:44 | Obračun amortizacije · Štampa amortizacije · Knjiženje amortizacije · Promena stopa amortiz. |
| Revalorizacija | `meni6`, REVALOR.PRG:41 | Obračun revalorizacije · Štampa revalorizacije · Knjiženje revalorizacije |
| Popisne liste | `meni_pop`, POPIS.PRG:36 | Štampa praznih popisnih listi · po obračunskim jedinicama i kontima · po kontima · Rekapitulacija po kontima · Rekapitulacija po obračunskim jedinicama |

**Napomena o "knjiženju":** u ovom DOS programu "knjiženje" (`knjiz_amortiz`,
`knjiz_revaloriz`, `knjizsreds`, `knjizrashod`) znači isključivo *upis nove
kolone/reda u `KARTICA.DBF`* — interni obračunski dnevnik sredstva. **Ne
postoji veza sa FIN.CLP** (nema generisanja naloga u glavnu knjigu); to je
potvrđeno čitanjem `knjiz_amortiz()` (AMORTIZ.PRG:607) — funkcija samo
`append blank` u `KARTICA.DBF` i ne dodiruje `NALOG.DBF`/`KARTICA.DBF` iz
FIN modula. Vidi §7 za status ove veze u novom sistemu.

---

## 2. Mapiranje DBF → .NET modeli

Za razliku od ERPiFinansije, ovde postoje **dva uvozna puta** sa različitim
stepenom kompletnosti — vidi §3. Tabela ispod prikazuje **stvarna polja**
pročitana direktno iz DBF zaglavlja u `Kor25` (ne iz koda uvoznika).

| DBF (polja) | .NET model | Napomena |
| --- | --- | --- |
| `KORISNIC.DBF` (`KOR, IME, UL, BR, TEL, Z, FAX, U_FILE, GRUP`) | `Firma` | Samo `IME→Naziv` se pouzdano poklapa; `PIB`/`MB`/`GRAD`/`MESTO` **ne postoje** u ovoj šemi DBF-a (uvoznik ih traži, ali ostaju prazni — ručni unos posle uvoza) |
| `KONTPLAN.DBF` (`KONTO:N5, OPIS_KONTA:C30, ULICA_I_BR:C50, MESTO_I_BR:C50`) | `Dobavljac` | ✅ 1:1, sva polja mapirana |
| `SREDSTVA.DBF` (`SIFRA, NAZIV, OOUR, KONTO, OBRAC_JED, AMORT_GR1, AMORT_GR2, STOPA_AM, DAT_AKT, REVAL_GR, NABAVNA, OTPISANA, KOLICINA, INVEN_BR, BR_FAKTURE, DAT_FAKTUR, DOBAVLJAC, BR_NAL, GOD_NAL, VEZA_SA_SI`) | `Sredstvo` | Osnovna polja mapirana (`SIFRA→LegacySifra`, `NAZIV`, `NABAVNA`, `OTPISANA`, `STOPA_AM`, `AMORT_GR1`, `DAT_AKT`, `INVEN_BR`, `KONTO`). **Nemapirano**: `OOUR`, `REVAL_GR`, `DOBAVLJAC` (link na dobavljača — na nivou *sredstva* se ne uvozi, samo na nivou *prijave*, vidi ispod), `BR_NAL`/`GOD_NAL`, `VEZA_SA_SI` (slobodan tekstualni "link" kod koji korisnik ručno upisuje u Prijavi — nema odgovarajuću kolonu ni u jednom novom modelu) |
| `KARTICA.DBF` (`SIFRA, RED_BROJ, DATUM, OPIS_PROM, OBRAC_JED, KONTO, AMORT_GR1, AMORT_GR2, STOPA_AM, KOEFIC_REV, KOLICINA, NABAVNA, OTPISANA`) | `Kartica` | ✅ 1:1, sva polja mapirana (`SIFRA` preko `sredstvaMap` u `SredstvoId`) |
| `RASHOD.DBF` (`BR_NALOGA, RED_BROJ, SIFRA, KOD, KOD_TEXT, DATUM, DOKUM_BROJ, PODACI, OBRAC_JED, KNJIZEN`) | `Rashod` | ✅ 1:1; `KOD` → `TipoviPromena` enum (1=Rashodovanje..9=PovećanjeAmortizacije), sa fallback na `Rashodovanje` ako kod nije prepoznat |
| `PRIJAVA.DBF` (`BR_NALOGA, RED_BROJ, SIFRA, NAZIV, OOUR, KONTO, OBRAC_JED, AMORT_GR1, AMORT_GR2, STOPA_AM, DAT_AKT, REVAL_GR, NABAVNA, OTPISANA, JED_MERE, KOLICINA, INVEN_BR, BR_FAKTURE, DAT_FAKTUR, DOBAVLJAC, BR_NAL, GOD_NAL, VEZA_SA_SI, KNJIZEN`) | `Prijava` | Vidi §3 — mapiranje zavisi od toga koji od dva uvoznika se koristi |
| `AMORTIZ.DBF`, `AMORT_D.DBF`, `REVALOR.DBF` | — | ❌ **namerno nije portovano** — istorijski snimci prošlih obračuna amortizacije/revalorizacije; novi sistem ih ne uvozi jer **ponovo računa** amortizaciju/revalorizaciju iz `Kartica` istorije umesto da kopira stare rezultate (vidi §5) |
| — (nema DBF-a) | `Komisija`, `Popis`, `PopisnaStavka`, `ClanKomisije` | ❌ nema legacy izvora — stari `POPIS.PRG` nije čuvao popisne podatke ni u jednom `.DBF`-u (§4); ovo je nova funkcionalnost |
| — (nema DBF-a) | `Korisnik` | Legacy login je čuvan u `KORISNIC.DBF`/posebnom mehanizmu unutar `GLAVNI.PRG` (`lozinka()`, `novikorisnik()`), nije uvezen — nova instalacija dobija seed admin naloga (isti obrazac kao ERPiFinansije/ERPiZarade) |

---

## 3. Dva uvozna puta — i njihova nesaglasnost

Za razliku od ERPiFinansije (jedan `DbfImportService`, CHANGELOG 1.0.9),
`ERPiSredstva` ima **dva nezavisna uvoznika** koji nisu sinhronizovani:

1. **`ERPiSredstvaMigration/Program.cs`** — samostalan konzolni alat,
   hardkodovan na jednu apsolutnu putanju (`C:\SREDSTVA\SREDS\KOR28\`) i
   šemu jednog konkretnog klijenta. Radi bez fallback imena kolona (traži
   tačno `NABAVNA`, `OTPISANA`, `INVEN_BR`...). Uvozi **i** `AmortizacionaGrupa2`,
   `RevalorizacionaGrupa`, `JedinicaMere`, `BrojNalaznice`, `BrNal`, `GodNal`
   u `Prijava`, ali **ne** postavlja `Prijava.DobavljacId`.
2. **`ERPiSredstvaApp/Services/DbfImportService.cs`** — uvoznik ugrađen u UI
   (Podešavanja → "Uvoz iz starog programa"), generičan preko liste
   alternativnih imena kolona (npr. `"NABAVNA", "NABVRED"`) i **ovo je
   stvarni, produkcioni put** koji krajnji korisnici koriste. On **postavlja**
   `Prijava.DobavljacId` (preko `KONTPLAN`→`Dobavljaci` mape po kontu), ali
   **ne** popunjava `AmortizacionaGrupa1/2`, `StopaAmortizacije`,
   `RevalorizacionaGrupa`, `JedinicaMere`, `BrojNalaznice`, `BrNal`, `GodNal`
   na uvezenoj `Prijava` (ostaju na podrazumevanoj vrednosti).

**Otvoreno pitanje:** ako se u budućnosti ponovo koristi
`ERPiSredstvaMigration` (npr. za novog klijenta sa DOS bazom), vredi mu
preneti `DobavljacId`-logiku iz `DbfImportService`, ili — bolje — ukloniti
dupliranje i svesti konzolni alat na tanku obertku oko istog
`DbfImportService` koji koristi UI, kao što je urađeno u ERPiFinansije.

---

## 4. Šta stari DOS program NIJE imao (a novi ima)

Poređenje `meni_pop` (POPIS.PRG:36) sa stvarnim sadržajem `Kor25` fascikle
pokazuje da **ne postoji nijedan `.DBF` fajl vezan za popis** — legacy
"Popisne liste" je bio **čisto štamparski** modul (5 varijanti izveštaja
direktno nad `SREDSTVA.DBF`), bez čuvanja rezultata popisa, komisija, ili
obrade viška/manjka. Svaki popis je bio papirni proces koji se posle ručno
unosio u knjige.

Nova verzija (`Komisija`/`Popis`/`PopisnaStavka`/`ClanKomisije`,
`PopisCalculator`) je prema tome **potpuno nova funkcionalnost bez legacy
pandana** — evidentira komisije, generiše popisne stavke sa knjiženim
stanjem, i obrađuje odstupanja (višak/manjak) kroz UI, što stari program
nikad nije radio digitalno.

Slično, **poreska amortizacija (Obrazac OA) i Poreski bilans (PB-1)** iz
README-a su **zakonska obaveza uvedena 2019.** (Pravilnik o Obrascu OA,
Zakon o porezu na dobit) — DOS program star preko dve decenije prirodno
nema legacy pandan za ove module; to nije "gap" nego funkcionalnost koja u
vreme pisanja Clipper koda nije ni postojala kao zakonski zahtev.

---

## 5. Ključni algoritmi — status portovanja

Za razliku od ERPiFinansije (gde je §7 spisak *za* portovanje), ovde su svi
ključni legacy algoritmi **već portovani i pokriveni xUnit testovima**
(`ERPiSredstvaData.Tests`), po istom principu ("ne izmišljati — preslikati
iz PRG, pa testom potvrditi"):

| Algoritam | Legacy | Novi servis | Test |
| --- | --- | --- | --- |
| Obračun amortizacije (linearna MRS 16 + degresivni saldo za sredstva pre 2019, čl. 4 i 7 Pravilnika) | `obracun_amortiz`, AMORTIZ.PRG:71 | `AmortizacijaCalculator` | `AmortizacijaCalculatorTests` |
| Poreska amortizacija (Obrazac OA, grupe I–V) | — (zakonska novina od 2019, nema legacy) | `PoreskaAmortizacijaCalculator` | `PoreskaAmortizacijaCalculatorTests` |
| Revalorizacija po koeficijentu | `obracun_revaloriz`, REVALOR.PRG:67 | `RevalorizacijaCalculator` | `RevalorizacijaCalculatorTests` |
| Popisne stavke i odstupanja | — (nema legacy, §4) | `PopisCalculator` | `PopisCalculatorTests` |

Detaljna pravila (rezidualna vrednost, `SrazmernoDanima`/`OdNarednogMeseca`,
pravilo "5 bruto zarada" za mali saldo grupe) su dokumentovana u skill-u
`serbian-depreciation-accounting-and-tax` — nije ponavljano ovde da ne bi
postalo dva izvora istine.

---

## 6. Veza sa ERPiFinansije (otvoreno, isto kao Finansije §6.1)

Kao što je zabeleženo u `ERPiFinansije/ANALIZA_I_PLAN.md` §6.1: **automatsko
knjiženje amortizacije u Glavnu knjigu** (Konto `5400` Troškovi amortizacije
/ `0290` Ispravka vrednosti) je i dalje **neurađeno** na obe strane —
potvrđeno pretragom (nema referenci na `AccountingDbContext` ni na
`ERPiFinansije` u `ERPiSredstvaData`/`ERPiSredstvaApp`). Ovo je i u DOS-u
bila ručna operacija (§1.2 — legacy "knjiženje" ne dodiruje FIN modul), pa
integracija nema legacy uzor za preslikavanje — mora se projektovati od
nule kad dođe na red (verovatno kao izvoz naloga iz ERPiSredstva koji se
uvozi/knjiži u ERPiFinansije, analogno postojećem obrascu `NalogId` veze
između robnih dokumenata i naloga opisanom u Finansije §9.2).

---

## 7. Preporučeni sledeći korak

Pošto je modul funkcionalno završen, prioriteti nisu fazna izgradnja nego:

1. **Uskladiti dva uvoznika** (§3) — bar preneti `DobavljacId` popunjavanje
   u `ERPiSredstvaMigration`, ili ga ukinuti u korist `DbfImportService`.
2. **Knjiženje amortizacije/revalorizacije u ERPiFinansije** (§6) — kad se
   za to ukaže stvarna potreba kod korisnika; nema legacy obrazac za
   preslikavanje, projektovati kao nov nalog-vezu.
3. Ostalo je održavanje — pratiti CHANGELOG.md za tekuće ispravke (npr.
   zaštita od pada pri otvaranju tuđe baze, 1.1.3).

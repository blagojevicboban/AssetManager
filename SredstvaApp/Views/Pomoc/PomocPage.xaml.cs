using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;

namespace SredstvaApp.Views.Pomoc;

public partial class PomocPage : Page
{
    private readonly List<PomocTema> _teme = new()
    {
        new PomocTema
        {
            Naslov = "👋 Dobrodošli u ERPi Sredstva",
            Sadrzaj =
                "Dobrodošli u sistem za Evidenciju i obračun osnovnih sredstava. Ova aplikacija menja stari (Clipper/DOS) softver i donosi moderno grafičko okruženje sa integrisanim sistemom za kreiranje i štampu PDF izveštaja.\n\n" +
                "NAPOMENA O STARIM PODACIMA:\n" +
                "Svi podaci iz starih programa automatski su migrirani. Stara šifra sredstva iz DBF datoteke čuva se unutar kartice sredstva pod opcijom 'Legacy Šifra'.\n\n" +
                "PRATITE TEME POMOĆI:\n" +
                "Sa leve strane izaberite željenu oblast da biste pročitali detaljna uputstva za rad sa sredstvima, prijavama, rashodima, amortizacijom i izveštajima."
        },
        new PomocTema
        {
            Naslov = "🔐 Prijava i korisnički nalozi",
            Sadrzaj =
                "Pristup aplikaciji je zaštićen korisničkim nalogom.\n\n" +
                "1. PRIJAVA:\n" +
                "• Pri pokretanju programa unesite korisničko ime i lozinku koje ste dobili od administratora.\n\n" +
                "2. ULOGE:\n" +
                "• Nalog Administrator ima pun pristup, uključujući upravljanje korisnicima.\n" +
                "• Nalog Operater radi sa sredstvima i nalozima, bez pristupa modulu Korisnici.\n\n" +
                "3. UPRAVLJANJE KORISNICIMA (meni '👥 Korisnici'):\n" +
                "• Administrator dodaje nove naloge, menja uloge, poništava zaboravljenu lozinku i deaktivira naloge zaposlenih koji više ne treba da imaju pristup.\n" +
                "• Nalog se deaktivira, a ne briše, kako bi istorija unosa ostala sačuvana.",
            Kljuc = "korisnici"
        },
        new PomocTema
        {
            Naslov = "🏢 Firme (rad sa više preduzeća)",
            Sadrzaj =
                "Meni '🏢 Upravljanje firmama' omogućava rad sa proizvoljnim brojem preduzeća u istoj instalaciji — svaka firma ima svoju odvojenu bazu podataka.\n\n" +
                "1. NOVA FIRMA:\n" +
                "• Dodajte novu firmu i unesite osnovne podatke (naziv, PIB, matični broj, adresa, bankovni račun...).\n\n" +
                "2. AKTIVACIJA:\n" +
                "• Klikom na '⭐ Postavi kao aktivnu' prebacujete se na bazu podataka izabrane firme.\n\n" +
                "Sve ostale stranice aplikacije (Osnovna sredstva, Amortizacija, Popis...) uvek prikazuju i menjaju podatke trenutno aktivne firme.",
            Kljuc = "firme"
        },
        new PomocTema
        {
            Naslov = "📊 Radna tabla (Dashboard)",
            Sadrzaj =
                "Po ulasku u preduzeće dočekaće vas Radna tabla sa grafičkim i numeričkim pregledom ključnih informacija.\n\n" +
                "• Zbirni podaci: Brz uvid u ukupan broj sredstava, kao i zbirnu nabavnu i sadašnju vrednost svih aktivnih sredstava u bazi.\n" +
                "• Grafikoni: Analiza strukture vrednosti po kontima ili pregled kojih 5 sredstava ima najveću sadašnju vrednost.\n" +
                "• Interaktivnost: Prelaskom kursora preko delova grafikona prikazuju se tačne numeričke vrednosti.",
            Kljuc = "dashboard"
        },
        new PomocTema
        {
            Naslov = "🏗️ Osnovna sredstva (Kartice)",
            Sadrzaj =
                "Meni '🏗️ Osnovna sredstva' omogućava upravljanje katalogom svih osnovnih sredstava preduzeća.\n\n" +
                "1. PREGLED:\n" +
                "• U tabeli se vidi stanje, nabavna i otpisana vrednost za svako sredstvo.\n\n" +
                "2. IZMENE:\n" +
                "• Dvoklik na red tabele ili klik na olovku otvara ekran za detaljno podešavanje stopa amortizacije i datuma aktivacije.\n\n" +
                "3. MASOVNA SELEKCIJA:\n" +
                "• Kućica u zaglavlju prve kolone (☑) selektuje ili poništava selekciju svih sredstava trenutno prikazanih u tabeli (poštuje aktivnu pretragu), radi bržeg označavanja za akcije poput štampe nalepnica.\n\n" +
                "4. PRIJAVE I RASHODI:\n" +
                "• Kroz dodatne module sredstvo može biti evidentirano kao trajno otuđeno (rashodovano).\n\n" +
                "5. ANALITIČKE KARTICE (meni '📋 Analitičke kartice'):\n" +
                "• Hronološki pregled svih promena (prijava, rashod, revalorizacija, amortizacija) za izabrano sredstvo.",
            Kljuc = "sredstva"
        },
        new PomocTema
        {
            Naslov = "🏢 Dobavljači",
            Sadrzaj =
                "Šifarnik dobavljača (po kontu) koristi se prilikom unosa Prijave sredstava.\n\n" +
                "• Pretražujte dobavljače po nazivu ili kontu preko polja za pretragu.\n" +
                "• Klikom na dobavljača u listi, na desnoj strani se prikazuju njegovi osnovni podaci (adresa, mesto) i pregled svih Prijava sredstava koje su na njega evidentirane.\n" +
                "• Dugmad '✏️ Izmeni' i '🗑️ Obriši' služe za ažuriranje šifarnika.",
            Kljuc = "dobavljaci"
        },
        new PomocTema
        {
            Naslov = "📥 Prijava sredstava",
            Sadrzaj =
                "Modul za evidenciju nabavke i aktiviranja novih osnovnih sredstava kroz naloge za prijavu.\n\n" +
                "• Klikom na '➕ Nova prijava' otvara se ekran za unos naloga — dobavljač, datum aktiviranja i stavke (sredstva sa količinom i nabavnom vrednošću).\n" +
                "• Leva tabela prikazuje sve naloge sa zbirnim podacima (broj stavki, ukupna nabavna vrednost, status knjiženja); desni panel prikazuje stavke naloga koji je trenutno selektovan.\n" +
                "• Dvoklikom na nalog ili klikom na '✏️ Uredi Nalog' otvara se nalog za dalju izmenu.",
            Kljuc = "prijava"
        },
        new PomocTema
        {
            Naslov = "📤 Rashod i promene",
            Sadrzaj =
                "Modul za evidenciju rashodovanja, prodaje, otuđenja, prenosa u drugu obračunsku jedinicu, brisanja i povećanja vrednosti/količine/amortizacije osnovnih sredstava.\n\n" +
                "• Statistika u vrhu stranice prikazuje broj naloga, broj rashodovanih i prodatih sredstava, kao i broj proknjiženih naloga i onih na čekanju.\n" +
                "• Klikom na '📤 Novi nalog' unosite novi nalog i birate tip promene (kod), sredstvo i datum.\n" +
                "• Leva tabela prikazuje sve naloge, obojene po dominantnom tipu promene; desni panel prikazuje stavke (sredstva) izabranog naloga, sa mogućnošću izmene preko '✏️ Uredi Nalog'.\n" +
                "• Dugme '🖨 Štampa' generiše PDF izveštaj za trenutno prikazane (filtrirane) naloge.",
            Kljuc = "rashod"
        },
        new PomocTema
        {
            Naslov = "📊 Računovodstvena i Poreska Amortizacija",
            Sadrzaj =
                "Modul za obračun računovodstvene amortizacije (po MRS 16) i poreske amortizacije (po Pravilniku o poreskoj amortizaciji i Zakonu o porezu na dobit).\n\n" +
                "1. RAČUNOVODSTVENA AMORTIZACIJA (MRS 16):\n" +
                "• Podržan unos rezidualne (spasavajuće) vrednosti — osnovica za amortizaciju je Nabavna − Rezidualna vrednost i sredstvo se ne otpisuje ispod rezidualnog iznosa.\n" +
                "• Izbor pravila početka amortizacije: 'Srazmerno danima' (od tačnog datuma aktiviranja) ili 'Od narednog meseca'.\n" +
                "• Brzi odabir perioda: Godišnji obračun, kvartalni (Q1–Q4) ili proizvoljni mesečni period sa automatskim opisnim dnevnikom knjiženja.\n" +
                "• Štampa detaljnih PDF izveštaja po kontima i obračunskim jedinicama (RJ).\n\n" +
                "2. PORESKA AMORTIZACIJA (Obrazac OA):\n" +
                "• Pojedinačna proporcionalna poreska amortizacija za sredstva stvorena/nabavljena od 1.1.2019, razvrstana u zakonske grupe I (2,5%), II (10%), III (15%), IV (20%) i V (30%).\n" +
                "• Automatski asistent predlaže zakonsku grupu i stopu pri unosu sredstava.\n" +
                "• Dugme '🪄 Masovna Dodela Grupa' vrši masovnu dodelu grupa i stopa za sva postojeća sredstva u bazi sa prikazom rekapitulacije.\n" +
                "• Generisanje zvaničnog PDF izveštaja 'Obrazac OA'.\n\n" +
                "3. PORESKI BILANS (Obrazac PB-1):\n" +
                "• Praćenje i obračun privremenih poreskih razlika (Računovodstvena − Poreska amortizacija).\n" +
                "• Dugme 'PB-1 Izveštaj (PDF)' kreira zvanični izveštaj sa zbirnim podacima za unos u Poreski Bilans.\n\n" +
                "4. STARA SREDSTVA PRE 2019 (Čl. 4 i 7 Pravilnika):\n" +
                "• Obračun degresivne amortizacije na ukupni saldo grupa II–V.\n" +
                "• Automatska primena pravila 5 bruto zarada u RS: ako je krajnji saldo grupe manji od 5 prosečnih bruto zarada, celokupan saldo se priznaje kao rashod i saldo grupe postaje 0.\n\n" +
                "5. AUTOMATSKA AMORTIZACIJA PRI RASHODOVANJU:\n" +
                "• Prilikom rashodovanja u toku godine, sistem automatski obračunava i knjiži srazmernu računovodstvenu amortizaciju od početka godine do datuma rashoda pre samog storniranja.",
            Kljuc = "amortizacija"
        },
        new PomocTema
        {
            Naslov = "📈 Revalorizacija",
            Sadrzaj =
                "Revalorizacija se radi radi usklađivanja knjigovodstvene vrednosti sredstava sa inflacijom ili trenutnim tržišnim stanjem, primenom revalorizacionih koeficijenata.\n\n" +
                "⚠️ Pre izvođenja Revalorizacije morate uneti Godišnji i Mesečni koeficijent.\n\n" +
                "• Unesite željene parametre u input polja.\n" +
                "• Kliknite na dugme '⚙️ Obračunaj'.\n" +
                "• Program kreira novu liniju u tabeli koju kasnije možete Štampati kao PDF ili kliknuti Proknjiži kako bi se ažurirale vrednosti nad osnovnim sredstvima u glavnoj bazi.",
            Kljuc = "revalorizacija"
        },
        new PomocTema
        {
            Naslov = "📄 Popis",
            Sadrzaj =
                "Na kraju godine sprovodi se obavezni popis inventara radi utvrđivanja eventualnih odstupanja između knjigovodstvenog i stvarnog stanja.\n\n" +
                "1. KREIRANJE KOMISIJE:\n" +
                "• U tabu 'Komisije' dodajte novo ime popisne komisije, zatim unesite imena članova i njihove uloge (Predsednik/Član).\n\n" +
                "2. NOVI POPIS:\n" +
                "• Kreirajte novu popisnu listu (unosi se godina, datum popisa i bira komisija).\n\n" +
                "3. RAD NA TERENU:\n" +
                "• Selektujte novokreirani popis i kliknite na '🖨 Štampaj Praznu Listu' — program otvara PDF koji se štampa i deli radnicima da olovkom unesu stvarna stanja.\n\n" +
                "4. UNOS RAZLIKA:\n" +
                "• Dvoklikom na selektovani popis otvara se ekran gde se masovno unosi stvarna količina za svako sredstvo.\n\n" +
                "5. ZAVRŠNI IZVEŠTAJ:\n" +
                "• Klikom na '🖨 Štampaj Izveštaj o Popisu' dobija se detaljan račun viškova i manjkova objedinjen po kontima.",
            Kljuc = "popis"
        },
        new PomocTema
        {
            Naslov = "📑 Izveštaji i Rekapitulacije",
            Sadrzaj =
                "Meni '📑 Rekapitulacija' objedinjuje popisne i rekapitulacione preglede osnovnih sredstava, nezavisno od godišnjeg popisa.\n\n" +
                "• Sa leve strane izaberite željeni izveštaj: Popis svih sredstava ili neku od Rekapitulacija (grupisano po kriterijumu iz naziva izveštaja).\n" +
                "• Podaci se prikazuju u tabeli sa zbirnim vrednostima nabavne i sadašnje vrednosti u dnu ekrana.\n" +
                "• Dugme '💾 Export CSV' u gornjem desnom uglu snima trenutno prikazani izveštaj u CSV fajl (pogodno za dalju obradu u Excel-u).",
            Kljuc = "izvestaji"
        },
        new PomocTema
        {
            Naslov = "🏷️ Nalepnice i Bar-kodovi",
            Sadrzaj =
                "Za potrebe lakšeg popisa i identifikacije opreme, aplikacija omogućava štampu bar-kod nalepnica.\n\n" +
                "• Idite na meni '🏗️ Osnovna sredstva'.\n" +
                "• U tabeli štiklirajte kućice (u prvoj koloni) pored sredstava za koja želite da odštampate nalepnice — prethodno možete iskoristiti polje za pretragu, ili kućicu u zaglavlju kolone za brzo obeležavanje svih prikazanih sredstava.\n" +
                "• Kliknite na dugme '🖨 Nalepnice'.\n" +
                "• Program generiše PDF dokument (A4 format, 3 kolone) koji sadrži naziv firme, šifru, inventarski broj, naziv sredstva i CODE-128 bar-kod spreman za štampu."
        },
        new PomocTema
        {
            Naslov = "⚙️ Podešavanja",
            Sadrzaj =
                "Centralno mesto za rezervne kopije baze podataka, uvoz starih podataka i opšte postavke programa.\n\n" +
                "1. REZERVNA KOPIJA:\n" +
                "• Dugme '💾 Napravi rezervnu kopiju baze podataka' snima trenutno stanje aktivne firme na disk. Istorija svih dosadašnjih kopija je prikazana, sa mogućnošću brzog vraćanja klikom na 'Vrati bazu iz izabrane kopije'.\n\n" +
                "2. VRAĆANJE IZ FAJLA:\n" +
                "• Dugme '📂 Izaberi i vrati bazu podataka iz kopije' učitava proizvoljno sačuvanu kopiju.\n" +
                "⚠️ Vraćanje baze iz rezervne kopije u potpunosti zamenjuje sve trenutne podatke aktivne firme. Pre same zamene, program automatski pravi sigurnosnu kopiju trenutnog stanja.\n\n" +
                "3. AUTO-BACKUP:\n" +
                "• Padajući meni omogućava da se rezervna kopija pravi automatski — nikad, pri svakom izlasku iz programa, ili jednom dnevno.\n\n" +
                "4. UVOZ IZ STAROG PROGRAMA:\n" +
                "• U tabu 'Uvoz iz starog programa' izaberite folder legacy DOS/FoxPro instalacije. Program očitava listu firmi iz KORISNIC.DBF i omogućava uvoz izabranih firmi dugmetom '📥 Uvezi'.\n\n" +
                "5. POSTAVKE PROGRAMA:\n" +
                "• Uključite opciju da se program pri sledećem pokretanju automatski otvori maksimizovan.",
            Kljuc = "podesavanja"
        },
        new PomocTema
        {
            Naslov = "⌨️ Korisne prečice",
            Sadrzaj =
                "• F1 — Otvara Pomoć, direktno na temi koja odgovara trenutnoj stranici.\n" +
                "• Ctrl + M — Sklapa ili proširuje bočni navigacioni meni.\n" +
                "• Esc — Zatvara otvoreni modalni prozor (gde je podržano)."
        }
    };

    public PomocPage(string? initijalnaTema = null)
    {
        InitializeComponent();
        LstTeme.ItemsSource = _teme;

        var tema = initijalnaTema is not null ? _teme.FirstOrDefault(t => t.Kljuc == initijalnaTema) : null;
        LstTeme.SelectedItem = tema ?? (_teme.Count > 0 ? _teme[0] : null);
    }

    private void LstTeme_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (LstTeme.SelectedItem is PomocTema tema)
        {
            TxtNaslovTeme.Text = tema.Naslov;
            TxtSadrzajTeme.Text = tema.Sadrzaj;
        }
    }

    private void TxtPretragaTema_TextChanged(object sender, TextChangedEventArgs e)
    {
        var upit = TxtPretragaTema.Text?.Trim() ?? string.Empty;
        var prethodnaSelekcija = LstTeme.SelectedItem as PomocTema;

        var filtrirano = upit.Length == 0
            ? _teme
            : _teme.Where(t =>
                t.Naslov.Contains(upit, StringComparison.OrdinalIgnoreCase) ||
                t.Sadrzaj.Contains(upit, StringComparison.OrdinalIgnoreCase)).ToList();

        LstTeme.ItemsSource = filtrirano;

        if (prethodnaSelekcija is not null && filtrirano.Contains(prethodnaSelekcija))
            LstTeme.SelectedItem = prethodnaSelekcija;
        else if (filtrirano.Count > 0)
            LstTeme.SelectedIndex = 0;
        else
        {
            TxtNaslovTeme.Text = "Nema rezultata";
            TxtSadrzajTeme.Text = "Nijedna tema pomoći ne odgovara pretrazi.";
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SredstvaData.Migrations
{
    /// <inheritdoc />
    public partial class AddKorisnici : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Dobavljaci",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Konto = table.Column<int>(type: "INTEGER", nullable: false),
                    OpisKonta = table.Column<string>(type: "TEXT", nullable: false),
                    UlicaIBroj = table.Column<string>(type: "TEXT", nullable: false),
                    MestoIBroj = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Dobavljaci", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Firme",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Naziv = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Mesto = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    MaticniBroj = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    PIB = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Firme", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Komisije",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Naziv = table.Column<string>(type: "TEXT", nullable: false),
                    DatumKreiranja = table.Column<DateTime>(type: "TEXT", nullable: false),
                    JeAktivna = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Komisije", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Korisnici",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ImePrezime = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    KorisnickoIme = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    LozinkaHash = table.Column<string>(type: "TEXT", nullable: false),
                    Uloga = table.Column<int>(type: "INTEGER", nullable: false),
                    JeAktivan = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Korisnici", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Sredstva",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    InventarskiBroj = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Naziv = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    DatumNabavke = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DatumAktiviranja = table.Column<DateTime>(type: "TEXT", nullable: false),
                    NabavnaVrednost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IspravkaVrednosti = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SadasnjaVrednost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AmortizacionaGrupa = table.Column<string>(type: "TEXT", nullable: false),
                    StopaAmortizacije = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    JeAktivno = table.Column<bool>(type: "INTEGER", nullable: false),
                    Kolicina = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    LegacySifra = table.Column<int>(type: "INTEGER", nullable: false),
                    FirmaId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sredstva", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Sredstva_Firme_FirmaId",
                        column: x => x.FirmaId,
                        principalTable: "Firme",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ClanoviKomisije",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    KomisijaId = table.Column<int>(type: "INTEGER", nullable: false),
                    ImePrezime = table.Column<string>(type: "TEXT", nullable: false),
                    Uloga = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClanoviKomisije", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClanoviKomisije_Komisije_KomisijaId",
                        column: x => x.KomisijaId,
                        principalTable: "Komisije",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Popisi",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DatumPopisa = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Godina = table.Column<int>(type: "INTEGER", nullable: false),
                    KomisijaId = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Popisi", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Popisi_Komisije_KomisijaId",
                        column: x => x.KomisijaId,
                        principalTable: "Komisije",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Kartice",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SredstvoId = table.Column<int>(type: "INTEGER", nullable: false),
                    RedBroj = table.Column<int>(type: "INTEGER", nullable: false),
                    Datum = table.Column<DateTime>(type: "TEXT", nullable: false),
                    OpisPromene = table.Column<string>(type: "TEXT", nullable: false),
                    ObracunskaJedinica = table.Column<int>(type: "INTEGER", nullable: false),
                    Konto = table.Column<string>(type: "TEXT", nullable: false),
                    AmortizacionaGrupa1 = table.Column<int>(type: "INTEGER", nullable: false),
                    AmortizacionaGrupa2 = table.Column<int>(type: "INTEGER", nullable: false),
                    StopaAmortizacije = table.Column<decimal>(type: "TEXT", nullable: false),
                    KoeficijentRevalorizacije = table.Column<decimal>(type: "TEXT", nullable: false),
                    Kolicina = table.Column<decimal>(type: "TEXT", nullable: false),
                    NabavnaVrednost = table.Column<decimal>(type: "TEXT", nullable: false),
                    IspravkaVrednosti = table.Column<decimal>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Kartice", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Kartice_Sredstva_SredstvoId",
                        column: x => x.SredstvoId,
                        principalTable: "Sredstva",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Prijave",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BrojNaloga = table.Column<int>(type: "INTEGER", nullable: false),
                    RedBroj = table.Column<int>(type: "INTEGER", nullable: false),
                    SredstvoId = table.Column<int>(type: "INTEGER", nullable: false),
                    ObracunskaJedinica = table.Column<int>(type: "INTEGER", nullable: false),
                    Konto = table.Column<string>(type: "TEXT", nullable: false),
                    AmortizacionaGrupa1 = table.Column<int>(type: "INTEGER", nullable: false),
                    AmortizacionaGrupa2 = table.Column<int>(type: "INTEGER", nullable: false),
                    StopaAmortizacije = table.Column<decimal>(type: "TEXT", nullable: false),
                    DatumAktiviranja = table.Column<DateTime>(type: "TEXT", nullable: false),
                    RevalorizacionaGrupa = table.Column<int>(type: "INTEGER", nullable: false),
                    NabavnaVrednost = table.Column<decimal>(type: "TEXT", nullable: false),
                    OtpisanaVrednost = table.Column<decimal>(type: "TEXT", nullable: false),
                    JedinicaMere = table.Column<string>(type: "TEXT", nullable: false),
                    Kolicina = table.Column<decimal>(type: "TEXT", nullable: false),
                    InventarskiBroj = table.Column<string>(type: "TEXT", nullable: false),
                    BrojFakture = table.Column<string>(type: "TEXT", nullable: false),
                    DatumFakture = table.Column<DateTime>(type: "TEXT", nullable: true),
                    BrojNalaznice = table.Column<int>(type: "INTEGER", nullable: false),
                    BrNal = table.Column<string>(type: "TEXT", nullable: false),
                    GodNal = table.Column<int>(type: "INTEGER", nullable: false),
                    Knjizen = table.Column<bool>(type: "INTEGER", nullable: false),
                    DobavljacId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Prijave", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Prijave_Dobavljaci_DobavljacId",
                        column: x => x.DobavljacId,
                        principalTable: "Dobavljaci",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Prijave_Sredstva_SredstvoId",
                        column: x => x.SredstvoId,
                        principalTable: "Sredstva",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Rashodi",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BrojNaloga = table.Column<int>(type: "INTEGER", nullable: false),
                    RedBroj = table.Column<int>(type: "INTEGER", nullable: false),
                    SredstvoId = table.Column<int>(type: "INTEGER", nullable: false),
                    Kod = table.Column<int>(type: "INTEGER", nullable: false),
                    KodTekst = table.Column<string>(type: "TEXT", nullable: false),
                    Datum = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DokumentBroj = table.Column<string>(type: "TEXT", nullable: false),
                    Podaci = table.Column<decimal>(type: "TEXT", nullable: false),
                    ObracunskaJedinica = table.Column<int>(type: "INTEGER", nullable: false),
                    Knjizen = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rashodi", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Rashodi_Sredstva_SredstvoId",
                        column: x => x.SredstvoId,
                        principalTable: "Sredstva",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PopisneStavke",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PopisId = table.Column<int>(type: "INTEGER", nullable: false),
                    SredstvoId = table.Column<int>(type: "INTEGER", nullable: false),
                    KnjiznaKolicina = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PopisanaKolicina = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    KnjiznaVrednost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ProcenjenaVrednost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Napomena = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PopisneStavke", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PopisneStavke_Popisi_PopisId",
                        column: x => x.PopisId,
                        principalTable: "Popisi",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PopisneStavke_Sredstva_SredstvoId",
                        column: x => x.SredstvoId,
                        principalTable: "Sredstva",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Korisnici",
                columns: new[] { "Id", "ImePrezime", "JeAktivan", "KorisnickoIme", "LozinkaHash", "Uloga" },
                values: new object[] { 1, "Administrator", true, "admin", "jGl25bVBBBW96Qi9Te4V37Fnqchz/Eu4qB9vKrRIqRg=", 0 });

            migrationBuilder.CreateIndex(
                name: "IX_ClanoviKomisije_KomisijaId",
                table: "ClanoviKomisije",
                column: "KomisijaId");

            migrationBuilder.CreateIndex(
                name: "IX_Kartice_SredstvoId",
                table: "Kartice",
                column: "SredstvoId");

            migrationBuilder.CreateIndex(
                name: "IX_Popisi_KomisijaId",
                table: "Popisi",
                column: "KomisijaId");

            migrationBuilder.CreateIndex(
                name: "IX_PopisneStavke_PopisId",
                table: "PopisneStavke",
                column: "PopisId");

            migrationBuilder.CreateIndex(
                name: "IX_PopisneStavke_SredstvoId",
                table: "PopisneStavke",
                column: "SredstvoId");

            migrationBuilder.CreateIndex(
                name: "IX_Prijave_DobavljacId",
                table: "Prijave",
                column: "DobavljacId");

            migrationBuilder.CreateIndex(
                name: "IX_Prijave_SredstvoId",
                table: "Prijave",
                column: "SredstvoId");

            migrationBuilder.CreateIndex(
                name: "IX_Rashodi_SredstvoId",
                table: "Rashodi",
                column: "SredstvoId");

            migrationBuilder.CreateIndex(
                name: "IX_Sredstva_FirmaId",
                table: "Sredstva",
                column: "FirmaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClanoviKomisije");

            migrationBuilder.DropTable(
                name: "Kartice");

            migrationBuilder.DropTable(
                name: "Korisnici");

            migrationBuilder.DropTable(
                name: "PopisneStavke");

            migrationBuilder.DropTable(
                name: "Prijave");

            migrationBuilder.DropTable(
                name: "Rashodi");

            migrationBuilder.DropTable(
                name: "Popisi");

            migrationBuilder.DropTable(
                name: "Dobavljaci");

            migrationBuilder.DropTable(
                name: "Sredstva");

            migrationBuilder.DropTable(
                name: "Komisije");

            migrationBuilder.DropTable(
                name: "Firme");
        }
    }
}

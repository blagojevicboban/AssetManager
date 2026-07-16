using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SredstvaData.Migrations
{
    /// <inheritdoc />
    public partial class DodatiKontoObracunskaJedinica : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Sredstva_Firme_FirmaId",
                table: "Sredstva");

            migrationBuilder.DropIndex(
                name: "IX_Sredstva_FirmaId",
                table: "Sredstva");

            migrationBuilder.RenameColumn(
                name: "FirmaId",
                table: "Sredstva",
                newName: "ObracunskaJedinica");

            migrationBuilder.AddColumn<string>(
                name: "Konto",
                table: "Sredstva",
                type: "TEXT",
                maxLength: 20,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Konto",
                table: "Sredstva");

            migrationBuilder.RenameColumn(
                name: "ObracunskaJedinica",
                table: "Sredstva",
                newName: "FirmaId");

            migrationBuilder.CreateIndex(
                name: "IX_Sredstva_FirmaId",
                table: "Sredstva",
                column: "FirmaId");

            migrationBuilder.AddForeignKey(
                name: "FK_Sredstva_Firme_FirmaId",
                table: "Sredstva",
                column: "FirmaId",
                principalTable: "Firme",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERPiSredstvaData.Migrations
{
    /// <inheritdoc />
    public partial class DodajPoreskaPoljaSredstva : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PoreskaGrupa",
                table: "Sredstva",
                type: "TEXT",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "PoreskaIspravkaVrednosti",
                table: "Sredstva",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PoreskaNabavnaVrednost",
                table: "Sredstva",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PoreskaStopa",
                table: "Sredstva",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "RezidualnaVrednost",
                table: "Sredstva",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PoreskaGrupa",
                table: "Sredstva");

            migrationBuilder.DropColumn(
                name: "PoreskaIspravkaVrednosti",
                table: "Sredstva");

            migrationBuilder.DropColumn(
                name: "PoreskaNabavnaVrednost",
                table: "Sredstva");

            migrationBuilder.DropColumn(
                name: "PoreskaStopa",
                table: "Sredstva");

            migrationBuilder.DropColumn(
                name: "RezidualnaVrednost",
                table: "Sredstva");
        }
    }
}

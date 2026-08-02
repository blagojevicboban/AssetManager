using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERPiSredstvaData.Migrations
{
    /// <inheritdoc />
    public partial class UpdateAdminSeedHash : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Uslovno: dira samo red koji i dalje ima originalni (neosoljeni) seed heš za "admin",
            // da ne bi presnimio lozinku koju je korisnik u međuvremenu promenio.
            migrationBuilder.Sql(@"
                UPDATE Korisnici
                SET LozinkaHash = 'PBKDF2$100000$9HpsWOyoV9tk7boQMPu8Iw==$tKuZniNJrMWGpwsjSJQrN7wSaeHWIxO+c8lXgvB5hzY='
                WHERE Id = 1 AND LozinkaHash = 'jGl25bVBBBW96Qi9Te4V37Fnqchz/Eu4qB9vKrRIqRg=';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                UPDATE Korisnici
                SET LozinkaHash = 'jGl25bVBBBW96Qi9Te4V37Fnqchz/Eu4qB9vKrRIqRg='
                WHERE Id = 1 AND LozinkaHash = 'PBKDF2$100000$9HpsWOyoV9tk7boQMPu8Iw==$tKuZniNJrMWGpwsjSJQrN7wSaeHWIxO+c8lXgvB5hzY=';");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AddressLibrary.Migrations
{
    /// <inheritdoc />
    public partial class DodanieTerytUlicPoprawki : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TerytUlicPoprawki",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Prefiks = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, comment: "Prefiks nazwy ulicy"),
                    Tytul = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true, comment: "Tytuł osoby (np. doktora, profesora)"),
                    Imie = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true, comment: "Pierwsze imię patrona ulicy"),
                    Imie2 = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true, comment: "Drugie imię patrona ulicy"),
                    Nazwisko = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true, comment: "Pierwsze nazwisko patrona ulicy"),
                    Nazwisko2 = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true, comment: "Drugie nazwisko patrona ulicy"),
                    Postfiks = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true, comment: "Postfiks/przydomek (np. Zapory, Zośki)")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TerytUlicPoprawki", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TerytUlicPoprawki_Nazwisko",
                table: "TerytUlicPoprawki",
                column: "Nazwisko");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TerytUlicPoprawki");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AddressLibrary.Migrations
{
    /// <inheritdoc />
    public partial class dodanie_typy_ulic : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TypyUlic",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Prefiks = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, comment: "Prefiks nazwy ulicy (np. płk., gen., ks., im.)"),
                    Tytul = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true, comment: "Tytuł osoby (np. dr., prof., płk.)"),
                    Imie = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true, comment: "Pierwsze imię patrona ulicy"),
                    Imie2 = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true, comment: "Drugie imię patrona ulicy (np. Kamil w Krzysztofa Kamila Baczyńskiego)"),
                    Nazwisko = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true, comment: "Pierwsze nazwisko patrona ulicy"),
                    Nazwisko2 = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true, comment: "Drugie nazwisko patrona ulicy (np. Reymonta w Władysława Stanisława Reymonta)"),
                    Postfiks = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true, comment: "Postfiks/przydomek (np. Zapory w Hieronima Dekutowskiego Zapory, Zośki w Tadeusza Zawadzkiego Zośki)")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TypyUlic", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TypyUlic_Imie_Nazwisko",
                table: "TypyUlic",
                columns: new[] { "Imie", "Nazwisko" });

            migrationBuilder.CreateIndex(
                name: "IX_TypyUlic_Nazwisko",
                table: "TypyUlic",
                column: "Nazwisko");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TypyUlic");
        }
    }
}

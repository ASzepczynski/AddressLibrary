using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AddressLibrary.Migrations
{
    /// <inheritdoc />
    public partial class initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Adresy",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    Komentarz = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Kraj = table.Column<string>(type: "nvarchar(100)", nullable: true),
                    Kod = table.Column<string>(type: "nvarchar(10)", nullable: true),
                    Miasto = table.Column<string>(type: "nvarchar(200)", nullable: true),
                    Ulica = table.Column<string>(type: "nvarchar(200)", nullable: true),
                    NrDomu = table.Column<string>(type: "nvarchar(20)", nullable: true),
                    NrLokalu = table.Column<string>(type: "nvarchar(20)", nullable: true),
                    Wojewodztwo = table.Column<string>(type: "nvarchar(100)", nullable: true),
                    Powiat = table.Column<string>(type: "nvarchar(100)", nullable: true),
                    Gmina = table.Column<string>(type: "nvarchar(100)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Adresy", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Pna",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Kod = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Miasto = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Dzielnica = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Ulica = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Gmina = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Powiat = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Wojewodztwo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Numery = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pna", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RodzajeGmin",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Kod = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false),
                    Nazwa = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RodzajeGmin", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RodzajeMiast",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Kod = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Nazwa = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RodzajeMiast", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TerytSimc",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Wojewodztwo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Powiat = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Gmina = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RodzajGminy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RodzajMiasta = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Mz = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Nazwa = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Symbol = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SymbolPodstawowy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StanNa = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TerytSimc", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TerytTerc",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Wojewodztwo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Powiat = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Gmina = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RodzajGminy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Nazwa = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NazwaDodatkowa = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StanNa = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TerytTerc", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TerytUlic",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Wojewodztwo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Powiat = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Gmina = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RodzajGminy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Symbol = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SymbolUlicy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Cecha = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Nazwa1 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Nazwa2 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StanNa = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TerytUlic", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TerytUlicPoprawki",
                columns: table => new
                {
                    DbId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Id = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false, comment: "Identyfikator/klucz biznesowy - oryginalna pełna nazwa ulicy: Cecha + Nazwa2 + Nazwa1"),
                    Cecha = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true, comment: "Cecha ulicy (np. ul., al., pl.)"),
                    Prefiks = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true, comment: "Prefiks nazwy ulicy (imienia, leśny)"),
                    Tytul = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true, comment: "Tytuł osoby (np. dr., prof., płk.)"),
                    Imie = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true, comment: "Pierwsze imię patrona ulicy"),
                    Imie2 = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true, comment: "Drugie imię patrona ulicy"),
                    Nazwisko = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true, comment: "Pierwsze nazwisko patrona ulicy"),
                    Nazwisko2 = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true, comment: "Drugie nazwisko patrona ulicy"),
                    Pseudonim = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true, comment: "Pseudonim patrona ulicy (np. Zapory, Zośki, Nila)"),
                    Postfiks = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true, comment: "Postfiks/przydomek (dodatkowe informacje)")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TerytUlicPoprawki", x => x.DbId);
                });

            migrationBuilder.CreateTable(
                name: "TerytWmRodz",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RozdzajMiasta = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Nazwa = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StanNa = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TerytWmRodz", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TypyUlic",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Prefiks = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true, comment: "Prefiks nazwy ulicy (im., Leśny, Miejski)"),
                    Tytul = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true, comment: "Tytuł osoby (np. dr., prof., płk.)"),
                    Imie = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true, comment: "Pierwsze imię patrona ulicy"),
                    Imie2 = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true, comment: "Drugie imię patrona ulicy (np. Kamil w Krzysztofa Kamila Baczyńskiego)"),
                    Nazwisko = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true, comment: "Pierwsze nazwisko patrona ulicy"),
                    Nazwisko2 = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true, comment: "Drugie nazwisko patrona ulicy (np. Reymonta w Władysława Stanisława Reymonta)"),
                    Pseudonim = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true, comment: "Pseudonim patrona ulicy (np. Zapory, Zośki, Nila)"),
                    Postfiks = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true, comment: "Postfiks/przydomek (dodatkowe informacje)")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TypyUlic", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Wojewodztwa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Kod = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false),
                    Nazwa = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Wojewodztwa", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Powiaty",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Kod = table.Column<string>(type: "nvarchar(4)", maxLength: 4, nullable: false),
                    Nazwa = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    WojewodztwoId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Powiaty", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Powiaty_Wojewodztwa_WojewodztwoId",
                        column: x => x.WojewodztwoId,
                        principalTable: "Wojewodztwa",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Gminy",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Kod = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: false),
                    Nazwa = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PowiatId = table.Column<int>(type: "int", nullable: false),
                    RodzajGminyId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Gminy", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Gminy_Powiaty_PowiatId",
                        column: x => x.PowiatId,
                        principalTable: "Powiaty",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Gminy_RodzajeGmin_RodzajGminyId",
                        column: x => x.RodzajGminyId,
                        principalTable: "RodzajeGmin",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Miasta",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Symbol = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: false),
                    Nazwa = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    GminaId = table.Column<int>(type: "int", nullable: false),
                    RodzajMiastaId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Miasta", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Miasta_Gminy_GminaId",
                        column: x => x.GminaId,
                        principalTable: "Gminy",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Miasta_RodzajeMiast_RodzajMiastaId",
                        column: x => x.RodzajMiastaId,
                        principalTable: "RodzajeMiast",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Ulice",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Symbol = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Cecha = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    Dzielnica = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    MiastoId = table.Column<int>(type: "int", nullable: false),
                    TypUlicyId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ulice", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Ulice_Miasta_MiastoId",
                        column: x => x.MiastoId,
                        principalTable: "Miasta",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Ulice_TypyUlic_TypUlicyId",
                        column: x => x.TypUlicyId,
                        principalTable: "TypyUlic",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "KodyPocztowe",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Kod = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: false),
                    Numery = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MiastoId = table.Column<int>(type: "int", nullable: false),
                    UlicaId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KodyPocztowe", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KodyPocztowe_Miasta_MiastoId",
                        column: x => x.MiastoId,
                        principalTable: "Miasta",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KodyPocztowe_Ulice_UlicaId",
                        column: x => x.UlicaId,
                        principalTable: "Ulice",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UrzedySkarbowe",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nazwa = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Kod = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    Miasto = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Ulica = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    NrDomu = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    UlicaId = table.Column<int>(type: "int", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Www = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UrzedySkarbowe", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UrzedySkarbowe_Ulice_UlicaId",
                        column: x => x.UlicaId,
                        principalTable: "Ulice",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Adresy_Kod",
                table: "Adresy",
                column: "Kod");

            migrationBuilder.CreateIndex(
                name: "IX_Adresy_Miasto_Kod",
                table: "Adresy",
                columns: new[] { "Miasto", "Kod" });

            migrationBuilder.CreateIndex(
                name: "IX_Gminy_Nazwa",
                table: "Gminy",
                column: "Nazwa");

            migrationBuilder.CreateIndex(
                name: "IX_Gminy_PowiatId_Kod_RodzajGminyId",
                table: "Gminy",
                columns: new[] { "PowiatId", "Kod", "RodzajGminyId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Gminy_RodzajGminyId",
                table: "Gminy",
                column: "RodzajGminyId");

            migrationBuilder.CreateIndex(
                name: "IX_KodyPocztowe_Kod",
                table: "KodyPocztowe",
                column: "Kod");

            migrationBuilder.CreateIndex(
                name: "IX_KodyPocztowe_MiastoId",
                table: "KodyPocztowe",
                column: "MiastoId");

            migrationBuilder.CreateIndex(
                name: "IX_KodyPocztowe_UlicaId",
                table: "KodyPocztowe",
                column: "UlicaId");

            migrationBuilder.CreateIndex(
                name: "IX_Miasta_GminaId",
                table: "Miasta",
                column: "GminaId");

            migrationBuilder.CreateIndex(
                name: "IX_Miasta_Nazwa",
                table: "Miasta",
                column: "Nazwa");

            migrationBuilder.CreateIndex(
                name: "IX_Miasta_RodzajMiastaId",
                table: "Miasta",
                column: "RodzajMiastaId");

            migrationBuilder.CreateIndex(
                name: "IX_Miasta_Symbol",
                table: "Miasta",
                column: "Symbol",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Powiaty_Nazwa",
                table: "Powiaty",
                column: "Nazwa");

            migrationBuilder.CreateIndex(
                name: "IX_Powiaty_WojewodztwoId_Kod",
                table: "Powiaty",
                columns: new[] { "WojewodztwoId", "Kod" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RodzajeGmin_Kod",
                table: "RodzajeGmin",
                column: "Kod",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RodzajeMiast_Kod",
                table: "RodzajeMiast",
                column: "Kod",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TerytUlicPoprawki_Id",
                table: "TerytUlicPoprawki",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_TerytUlicPoprawki_Nazwisko",
                table: "TerytUlicPoprawki",
                column: "Nazwisko");

            migrationBuilder.CreateIndex(
                name: "IX_TypyUlic_Imie_Nazwisko",
                table: "TypyUlic",
                columns: new[] { "Imie", "Nazwisko" });

            migrationBuilder.CreateIndex(
                name: "IX_TypyUlic_Nazwisko",
                table: "TypyUlic",
                column: "Nazwisko");

            migrationBuilder.CreateIndex(
                name: "IX_Ulice_MiastoId",
                table: "Ulice",
                column: "MiastoId");

            migrationBuilder.CreateIndex(
                name: "IX_Ulice_Symbol_MiastoId_Dzielnica",
                table: "Ulice",
                columns: new[] { "Symbol", "MiastoId", "Dzielnica" },
                unique: true,
                filter: "[Dzielnica] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Ulice_TypUlicyId",
                table: "Ulice",
                column: "TypUlicyId");

            migrationBuilder.CreateIndex(
                name: "IX_UrzedySkarbowe_Kod",
                table: "UrzedySkarbowe",
                column: "Kod");

            migrationBuilder.CreateIndex(
                name: "IX_UrzedySkarbowe_Miasto",
                table: "UrzedySkarbowe",
                column: "Miasto");

            migrationBuilder.CreateIndex(
                name: "IX_UrzedySkarbowe_Miasto_Ulica",
                table: "UrzedySkarbowe",
                columns: new[] { "Miasto", "Ulica" });

            migrationBuilder.CreateIndex(
                name: "IX_UrzedySkarbowe_Nazwa",
                table: "UrzedySkarbowe",
                column: "Nazwa");

            migrationBuilder.CreateIndex(
                name: "IX_UrzedySkarbowe_UlicaId",
                table: "UrzedySkarbowe",
                column: "UlicaId");

            migrationBuilder.CreateIndex(
                name: "IX_Wojewodztwa_Kod",
                table: "Wojewodztwa",
                column: "Kod",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Wojewodztwa_Nazwa",
                table: "Wojewodztwa",
                column: "Nazwa");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Adresy");

            migrationBuilder.DropTable(
                name: "KodyPocztowe");

            migrationBuilder.DropTable(
                name: "Pna");

            migrationBuilder.DropTable(
                name: "TerytSimc");

            migrationBuilder.DropTable(
                name: "TerytTerc");

            migrationBuilder.DropTable(
                name: "TerytUlic");

            migrationBuilder.DropTable(
                name: "TerytUlicPoprawki");

            migrationBuilder.DropTable(
                name: "TerytWmRodz");

            migrationBuilder.DropTable(
                name: "UrzedySkarbowe");

            migrationBuilder.DropTable(
                name: "Ulice");

            migrationBuilder.DropTable(
                name: "Miasta");

            migrationBuilder.DropTable(
                name: "TypyUlic");

            migrationBuilder.DropTable(
                name: "Gminy");

            migrationBuilder.DropTable(
                name: "RodzajeMiast");

            migrationBuilder.DropTable(
                name: "Powiaty");

            migrationBuilder.DropTable(
                name: "RodzajeGmin");

            migrationBuilder.DropTable(
                name: "Wojewodztwa");
        }
    }
}

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
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false, collation: "Polish_CS_AS"),
                    Komentarz = table.Column<string>(type: "nvarchar(max)", nullable: false, collation: "Polish_CS_AS"),
                    Kraj = table.Column<string>(type: "nvarchar(max)", nullable: false, collation: "Polish_CS_AS"),
                    Kod = table.Column<string>(type: "nvarchar(max)", nullable: false, collation: "Polish_CS_AS"),
                    Miasto = table.Column<string>(type: "nvarchar(max)", nullable: false, collation: "Polish_CS_AS"),
                    Ulica = table.Column<string>(type: "nvarchar(max)", nullable: false, collation: "Polish_CS_AS"),
                    NrDomu = table.Column<string>(type: "nvarchar(max)", nullable: false, collation: "Polish_CS_AS"),
                    NrLokalu = table.Column<string>(type: "nvarchar(max)", nullable: false, collation: "Polish_CS_AS"),
                    Wojewodztwo = table.Column<string>(type: "nvarchar(max)", nullable: false, collation: "Polish_CS_AS"),
                    Powiat = table.Column<string>(type: "nvarchar(max)", nullable: false, collation: "Polish_CS_AS"),
                    Gmina = table.Column<string>(type: "nvarchar(max)", nullable: false, collation: "Polish_CS_AS")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Adresy", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CechyUlic",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nazwa = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, collation: "Polish_CS_AS"),
                    Skrot = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, collation: "Polish_CS_AS")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CechyUlic", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Pna",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Kod = table.Column<string>(type: "nvarchar(max)", nullable: false, collation: "Polish_CS_AS"),
                    Miasto = table.Column<string>(type: "nvarchar(max)", nullable: false, collation: "Polish_CS_AS"),
                    Dzielnica = table.Column<string>(type: "nvarchar(max)", nullable: false, collation: "Polish_CS_AS"),
                    Ulica = table.Column<string>(type: "nvarchar(max)", nullable: false, collation: "Polish_CS_AS"),
                    Gmina = table.Column<string>(type: "nvarchar(max)", nullable: false, collation: "Polish_CS_AS"),
                    Powiat = table.Column<string>(type: "nvarchar(max)", nullable: false, collation: "Polish_CS_AS"),
                    Wojewodztwo = table.Column<string>(type: "nvarchar(max)", nullable: false, collation: "Polish_CS_AS"),
                    Numery = table.Column<string>(type: "nvarchar(max)", nullable: false, collation: "Polish_CS_AS")
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
                    Kod = table.Column<string>(type: "nvarchar(max)", nullable: false, collation: "Polish_CS_AS"),
                    Nazwa = table.Column<string>(type: "nvarchar(max)", nullable: false, collation: "Polish_CS_AS")
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
                    Kod = table.Column<string>(type: "nvarchar(max)", nullable: false, collation: "Polish_CS_AS"),
                    Nazwa = table.Column<string>(type: "nvarchar(max)", nullable: false, collation: "Polish_CS_AS")
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
                    Wojewodztwo = table.Column<string>(type: "nvarchar(max)", nullable: false, collation: "Polish_CS_AS"),
                    Powiat = table.Column<string>(type: "nvarchar(max)", nullable: false, collation: "Polish_CS_AS"),
                    Gmina = table.Column<string>(type: "nvarchar(max)", nullable: false, collation: "Polish_CS_AS"),
                    RodzajGminy = table.Column<string>(type: "nvarchar(max)", nullable: false, collation: "Polish_CS_AS"),
                    RodzajMiasta = table.Column<string>(type: "nvarchar(max)", nullable: false, collation: "Polish_CS_AS"),
                    Mz = table.Column<string>(type: "nvarchar(max)", nullable: false, collation: "Polish_CS_AS"),
                    Nazwa = table.Column<string>(type: "nvarchar(max)", nullable: false, collation: "Polish_CS_AS"),
                    Symbol = table.Column<string>(type: "nvarchar(max)", nullable: false, collation: "Polish_CS_AS"),
                    SymbolPodstawowy = table.Column<string>(type: "nvarchar(max)", nullable: false, collation: "Polish_CS_AS"),
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
                    Wojewodztwo = table.Column<string>(type: "nvarchar(max)", nullable: false, collation: "Polish_CS_AS"),
                    Powiat = table.Column<string>(type: "nvarchar(max)", nullable: false, collation: "Polish_CS_AS"),
                    Gmina = table.Column<string>(type: "nvarchar(max)", nullable: false, collation: "Polish_CS_AS"),
                    RodzajGminy = table.Column<string>(type: "nvarchar(max)", nullable: false, collation: "Polish_CS_AS"),
                    Nazwa = table.Column<string>(type: "nvarchar(max)", nullable: false, collation: "Polish_CS_AS"),
                    NazwaDodatkowa = table.Column<string>(type: "nvarchar(max)", nullable: false, collation: "Polish_CS_AS"),
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
                    Wojewodztwo = table.Column<string>(type: "nvarchar(max)", nullable: false, collation: "Polish_CS_AS"),
                    Powiat = table.Column<string>(type: "nvarchar(max)", nullable: false, collation: "Polish_CS_AS"),
                    Gmina = table.Column<string>(type: "nvarchar(max)", nullable: false, collation: "Polish_CS_AS"),
                    RodzajGminy = table.Column<string>(type: "nvarchar(max)", nullable: false, collation: "Polish_CS_AS"),
                    Symbol = table.Column<string>(type: "nvarchar(max)", nullable: false, collation: "Polish_CS_AS"),
                    SymbolUlicy = table.Column<string>(type: "nvarchar(max)", nullable: false, collation: "Polish_CS_AS"),
                    Cecha = table.Column<string>(type: "nvarchar(max)", nullable: false, collation: "Polish_CS_AS"),
                    Nazwa1 = table.Column<string>(type: "nvarchar(max)", nullable: false, collation: "Polish_CS_AS"),
                    Nazwa2 = table.Column<string>(type: "nvarchar(max)", nullable: false, collation: "Polish_CS_AS"),
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
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TerytId = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false, collation: "Polish_CS_AS"),
                    Cecha = table.Column<string>(type: "nvarchar(max)", nullable: true, collation: "Polish_CS_AS"),
                    Prefiks = table.Column<string>(type: "nvarchar(max)", nullable: true, collation: "Polish_CS_AS"),
                    Tytul = table.Column<string>(type: "nvarchar(max)", nullable: true, collation: "Polish_CS_AS"),
                    Imie = table.Column<string>(type: "nvarchar(max)", nullable: true, collation: "Polish_CS_AS"),
                    Imie2 = table.Column<string>(type: "nvarchar(max)", nullable: true, collation: "Polish_CS_AS"),
                    Nazwisko = table.Column<string>(type: "nvarchar(450)", nullable: true, collation: "Polish_CS_AS"),
                    Nazwisko2 = table.Column<string>(type: "nvarchar(max)", nullable: true, collation: "Polish_CS_AS"),
                    Pseudonim = table.Column<string>(type: "nvarchar(max)", nullable: true, collation: "Polish_CS_AS"),
                    Postfiks = table.Column<string>(type: "nvarchar(max)", nullable: true, collation: "Polish_CS_AS")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TerytUlicPoprawki", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TerytWmRodz",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RodzajMiasta = table.Column<string>(type: "nvarchar(max)", nullable: false, collation: "Polish_CS_AS"),
                    Nazwa = table.Column<string>(type: "nvarchar(max)", nullable: false, collation: "Polish_CS_AS"),
                    StanNa = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TerytWmRodz", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TytulyStopnie",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nazwa = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, collation: "Polish_CS_AS"),
                    Skrot = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, collation: "Polish_CS_AS"),
                    Dopelniacz = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, collation: "Polish_CS_AS")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TytulyStopnie", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Wojewodztwa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Kod = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false, collation: "Polish_CS_AS"),
                    Nazwa = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, collation: "Polish_CS_AS")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Wojewodztwa", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TypyUlic",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Prefiks = table.Column<string>(type: "nvarchar(max)", nullable: false, collation: "Polish_CS_AS"),
                    TytulStopienId = table.Column<int>(type: "int", nullable: false),
                    Imie = table.Column<string>(type: "nvarchar(max)", nullable: false, collation: "Polish_CS_AS"),
                    Imie2 = table.Column<string>(type: "nvarchar(max)", nullable: false, collation: "Polish_CS_AS"),
                    Nazwisko = table.Column<string>(type: "nvarchar(max)", nullable: false, collation: "Polish_CS_AS"),
                    Nazwisko2 = table.Column<string>(type: "nvarchar(max)", nullable: false, collation: "Polish_CS_AS"),
                    Pseudonim = table.Column<string>(type: "nvarchar(max)", nullable: false, collation: "Polish_CS_AS"),
                    Postfiks = table.Column<string>(type: "nvarchar(max)", nullable: false, collation: "Polish_CS_AS")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TypyUlic", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TypyUlic_TytulyStopnie_TytulStopienId",
                        column: x => x.TytulStopienId,
                        principalTable: "TytulyStopnie",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Powiaty",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Kod = table.Column<string>(type: "nvarchar(4)", maxLength: 4, nullable: false, collation: "Polish_CS_AS"),
                    Nazwa = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, collation: "Polish_CS_AS"),
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
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Gminy",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Kod = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: false, collation: "Polish_CS_AS"),
                    Nazwa = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, collation: "Polish_CS_AS"),
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
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Gminy_RodzajeGmin_RodzajGminyId",
                        column: x => x.RodzajGminyId,
                        principalTable: "RodzajeGmin",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Miasta",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Kod = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: false, collation: "Polish_CS_AS"),
                    Nazwa = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false, collation: "Polish_CS_AS"),
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
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Miasta_RodzajeMiast_RodzajMiastaId",
                        column: x => x.RodzajMiastaId,
                        principalTable: "RodzajeMiast",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Ulice",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Symbol = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false, collation: "Polish_CS_AS"),
                    CechaUlicyId = table.Column<int>(type: "int", nullable: true),
                    Dzielnica = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false, collation: "Polish_CS_AS"),
                    MiastoId = table.Column<int>(type: "int", nullable: false),
                    TypUlicyId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ulice", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Ulice_CechyUlic_CechaUlicyId",
                        column: x => x.CechaUlicyId,
                        principalTable: "CechyUlic",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Ulice_Miasta_MiastoId",
                        column: x => x.MiastoId,
                        principalTable: "Miasta",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Ulice_TypyUlic_TypUlicyId",
                        column: x => x.TypUlicyId,
                        principalTable: "TypyUlic",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "KodyPocztowe",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Kod = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: false, collation: "Polish_CS_AS"),
                    Numery = table.Column<string>(type: "nvarchar(max)", nullable: false, collation: "Polish_CS_AS"),
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
                    Nazwa = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false, collation: "Polish_CS_AS"),
                    Kod = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false, collation: "Polish_CS_AS"),
                    Miasto = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, collation: "Polish_CS_AS"),
                    Ulica = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false, collation: "Polish_CS_AS"),
                    NrDomu = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, collation: "Polish_CS_AS"),
                    UlicaId = table.Column<int>(type: "int", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, collation: "Polish_CS_AS"),
                    Www = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false, collation: "Polish_CS_AS")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UrzedySkarbowe", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UrzedySkarbowe_Ulice_UlicaId",
                        column: x => x.UlicaId,
                        principalTable: "Ulice",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Gminy_PowiatId",
                table: "Gminy",
                column: "PowiatId");

            migrationBuilder.CreateIndex(
                name: "IX_Gminy_RodzajGminyId",
                table: "Gminy",
                column: "RodzajGminyId");

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
                name: "IX_Miasta_RodzajMiastaId",
                table: "Miasta",
                column: "RodzajMiastaId");

            migrationBuilder.CreateIndex(
                name: "IX_Powiaty_WojewodztwoId",
                table: "Powiaty",
                column: "WojewodztwoId");

            migrationBuilder.CreateIndex(
                name: "IX_TerytUlicPoprawki_Id",
                table: "TerytUlicPoprawki",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_TerytUlicPoprawki_Nazwisko",
                table: "TerytUlicPoprawki",
                column: "Nazwisko");

            migrationBuilder.CreateIndex(
                name: "IX_TypyUlic_TytulStopienId",
                table: "TypyUlic",
                column: "TytulStopienId");

            migrationBuilder.CreateIndex(
                name: "IX_Ulice_CechaUlicyId",
                table: "Ulice",
                column: "CechaUlicyId");

            migrationBuilder.CreateIndex(
                name: "IX_Ulice_MiastoId",
                table: "Ulice",
                column: "MiastoId");

            migrationBuilder.CreateIndex(
                name: "IX_Ulice_TypUlicyId",
                table: "Ulice",
                column: "TypUlicyId");

            migrationBuilder.CreateIndex(
                name: "IX_UrzedySkarbowe_UlicaId",
                table: "UrzedySkarbowe",
                column: "UlicaId");
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
                name: "CechyUlic");

            migrationBuilder.DropTable(
                name: "Miasta");

            migrationBuilder.DropTable(
                name: "TypyUlic");

            migrationBuilder.DropTable(
                name: "Gminy");

            migrationBuilder.DropTable(
                name: "RodzajeMiast");

            migrationBuilder.DropTable(
                name: "TytulyStopnie");

            migrationBuilder.DropTable(
                name: "Powiaty");

            migrationBuilder.DropTable(
                name: "RodzajeGmin");

            migrationBuilder.DropTable(
                name: "Wojewodztwa");
        }
    }
}

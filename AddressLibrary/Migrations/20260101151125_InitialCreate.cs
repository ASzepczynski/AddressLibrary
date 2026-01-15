using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AddressLibrary.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                    RodzajGminyId = table.Column<int>(type: "int", nullable: false),
                    PowiatId = table.Column<int>(type: "int", nullable: false)
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
                    Nazwa = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RodzajMiastaId = table.Column<int>(type: "int", nullable: true),
                    GminaId = table.Column<int>(type: "int", nullable: false)
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
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Ulice",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Symbol = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Cecha = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Nazwa1 = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Nazwa2 = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    MiastoId = table.Column<int>(type: "int", nullable: false)
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
                });

            migrationBuilder.CreateTable(
                name: "KodyPocztowe",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Kod = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: false),
                    Numery = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    MiastoId = table.Column<int>(type: "int", nullable: false),
                    UlicaId = table.Column<int>(type: "int", nullable: true)
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

            migrationBuilder.CreateIndex(
                name: "IX_Gminy_Kod",
                table: "Gminy",
                column: "Kod",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Gminy_PowiatId",
                table: "Gminy",
                column: "PowiatId");

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
                name: "IX_Miasta_RodzajMiastaId",
                table: "Miasta",
                column: "RodzajMiastaId");

            migrationBuilder.CreateIndex(
                name: "IX_Miasta_Symbol",
                table: "Miasta",
                column: "Symbol",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Powiaty_Kod",
                table: "Powiaty",
                column: "Kod",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Powiaty_WojewodztwoId",
                table: "Powiaty",
                column: "WojewodztwoId");

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
                name: "IX_Ulice_MiastoId",
                table: "Ulice",
                column: "MiastoId");

            migrationBuilder.CreateIndex(
                name: "IX_Ulice_Symbol",
                table: "Ulice",
                column: "Symbol");

            migrationBuilder.CreateIndex(
                name: "IX_Wojewodztwa_Kod",
                table: "Wojewodztwa",
                column: "Kod",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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
                name: "TerytWmRodz");

            migrationBuilder.DropTable(
                name: "Ulice");

            migrationBuilder.DropTable(
                name: "Miasta");

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

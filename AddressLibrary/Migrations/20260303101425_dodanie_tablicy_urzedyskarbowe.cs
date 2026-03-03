using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AddressLibrary.Migrations
{
    /// <inheritdoc />
    public partial class dodanie_tablicy_urzedyskarbowe : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UrzedySkarbowe",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nazwa = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    UlicaId = table.Column<int>(type: "int", nullable: false),
                    NrDomu = table.Column<string>(type: "nvarchar(20)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    Www = table.Column<string>(type: "nvarchar(200)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UrzedySkarbowe", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UrzedySkarbowe_Ulice_UlicaId",
                        column: x => x.UlicaId,
                        principalTable: "Ulice",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UrzedySkarbowe_Email",
                table: "UrzedySkarbowe",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_UrzedySkarbowe_Nazwa",
                table: "UrzedySkarbowe",
                column: "Nazwa");

            migrationBuilder.CreateIndex(
                name: "IX_UrzedySkarbowe_UlicaId",
                table: "UrzedySkarbowe",
                column: "UlicaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UrzedySkarbowe");
        }
    }
}

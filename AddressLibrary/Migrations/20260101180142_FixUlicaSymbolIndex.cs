using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AddressLibrary.Migrations
{
    /// <inheritdoc />
    public partial class FixUlicaSymbolIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Ulice_Symbol",
                table: "Ulice");

            migrationBuilder.CreateIndex(
                name: "IX_Ulice_Symbol_MiastoId",
                table: "Ulice",
                columns: new[] { "Symbol", "MiastoId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Ulice_Symbol_MiastoId",
                table: "Ulice");

            migrationBuilder.CreateIndex(
                name: "IX_Ulice_Symbol",
                table: "Ulice",
                column: "Symbol",
                unique: true);
        }
    }
}

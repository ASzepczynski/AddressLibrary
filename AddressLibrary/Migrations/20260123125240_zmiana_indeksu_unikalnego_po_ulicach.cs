using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AddressLibrary.Migrations
{
    /// <inheritdoc />
    public partial class zmiana_indeksu_unikalnego_po_ulicach : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Ulice_Symbol_MiastoId",
                table: "Ulice");

            migrationBuilder.CreateIndex(
                name: "IX_Ulice_Symbol_MiastoId_Dzielnica",
                table: "Ulice",
                columns: new[] { "Symbol", "MiastoId", "Dzielnica" },
                unique: true,
                filter: "[Dzielnica] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Ulice_Symbol_MiastoId_Dzielnica",
                table: "Ulice");

            migrationBuilder.CreateIndex(
                name: "IX_Ulice_Symbol_MiastoId",
                table: "Ulice",
                columns: new[] { "Symbol", "MiastoId" },
                unique: true);
        }
    }
}

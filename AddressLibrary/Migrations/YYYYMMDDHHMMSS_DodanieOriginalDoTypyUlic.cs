using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AddressLibrary.Migrations
{
    /// <inheritdoc />
    public partial class DodanieOriginalDoTerytUlicPoprawki : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Original",
                table: "TerytUlicPoprawki",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                comment: "Oryginalna pełna nazwa ulicy: Cecha + Nazwa2 + Nazwa1");

            // ✅ Dodaj indeks dla szybszego wyszukiwania
            migrationBuilder.CreateIndex(
                name: "IX_TerytUlicPoprawki_Original",
                table: "TerytUlicPoprawki",
                column: "Original");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TerytUlicPoprawki_Original",
                table: "TerytUlicPoprawki");

            migrationBuilder.DropColumn(
                name: "Original",
                table: "TerytUlicPoprawki");
        }
    }
}
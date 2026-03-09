using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AddressLibrary.Migrations
{
    /// <inheritdoc />
    public partial class DodanieOriginalDoTypyUlic : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Original",
                table: "TypyUlic",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                comment: "Oryginalna pełna nazwa ulicy: Cecha + Nazwa2 + Nazwa1");

            // ✅ Dodaj indeks dla szybszego wyszukiwania
            migrationBuilder.CreateIndex(
                name: "IX_TypyUlic_Original",
                table: "TypyUlic",
                column: "Original");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TypyUlic_Original",
                table: "TypyUlic");

            migrationBuilder.DropColumn(
                name: "Original",
                table: "TypyUlic");
        }
    }
}
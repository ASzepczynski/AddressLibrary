using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AddressLibrary.Migrations
{
    /// <inheritdoc />
    public partial class dodanodzielnicędoulic : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Powiaty_Kod",
                table: "Powiaty");

            migrationBuilder.DropIndex(
                name: "IX_Powiaty_WojewodztwoId",
                table: "Powiaty");

            migrationBuilder.DropIndex(
                name: "IX_Gminy_Kod",
                table: "Gminy");

            migrationBuilder.DropIndex(
                name: "IX_Gminy_PowiatId",
                table: "Gminy");

            migrationBuilder.AddColumn<string>(
                name: "Dzielnica",
                table: "Ulice",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Powiaty_WojewodztwoId_Kod",
                table: "Powiaty",
                columns: new[] { "WojewodztwoId", "Kod" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Gminy_PowiatId_Kod_RodzajGminyId",
                table: "Gminy",
                columns: new[] { "PowiatId", "Kod", "RodzajGminyId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Powiaty_WojewodztwoId_Kod",
                table: "Powiaty");

            migrationBuilder.DropIndex(
                name: "IX_Gminy_PowiatId_Kod_RodzajGminyId",
                table: "Gminy");

            migrationBuilder.DropColumn(
                name: "Dzielnica",
                table: "Ulice");

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
                name: "IX_Gminy_Kod",
                table: "Gminy",
                column: "Kod",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Gminy_PowiatId",
                table: "Gminy",
                column: "PowiatId");
        }
    }
}

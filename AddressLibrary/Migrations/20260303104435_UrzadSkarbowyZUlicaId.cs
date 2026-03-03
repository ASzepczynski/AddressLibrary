using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AddressLibrary.Migrations
{
    /// <inheritdoc />
    public partial class UrzadSkarbowyZUlicaId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "UlicaId",
                table: "UrzedySkarbowe",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_UrzedySkarbowe_UlicaId",
                table: "UrzedySkarbowe",
                column: "UlicaId");

            migrationBuilder.AddForeignKey(
                name: "FK_UrzedySkarbowe_Ulice_UlicaId",
                table: "UrzedySkarbowe",
                column: "UlicaId",
                principalTable: "Ulice",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UrzedySkarbowe_Ulice_UlicaId",
                table: "UrzedySkarbowe");

            migrationBuilder.DropIndex(
                name: "IX_UrzedySkarbowe_UlicaId",
                table: "UrzedySkarbowe");

            migrationBuilder.DropColumn(
                name: "UlicaId",
                table: "UrzedySkarbowe");
        }
    }
}

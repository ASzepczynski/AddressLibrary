using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AddressLibrary.Migrations
{
    /// <inheritdoc />
    public partial class DodanieReferencjiTypUlicyDoUlicy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TypUlicyId",
                table: "Ulice",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Ulice_TypUlicyId",
                table: "Ulice",
                column: "TypUlicyId");

            migrationBuilder.AddForeignKey(
                name: "FK_Ulice_TypyUlic_TypUlicyId",
                table: "Ulice",
                column: "TypUlicyId",
                principalTable: "TypyUlic",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Ulice_TypyUlic_TypUlicyId",
                table: "Ulice");

            migrationBuilder.DropIndex(
                name: "IX_Ulice_TypUlicyId",
                table: "Ulice");

            migrationBuilder.DropColumn(
                name: "TypUlicyId",
                table: "Ulice");
        }
    }
}

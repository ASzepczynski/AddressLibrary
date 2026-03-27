using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AddressLibrary.Migrations
{
    /// <inheritdoc />
    public partial class referencja_do_cechyulic : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Cecha",
                table: "Ulice");

            migrationBuilder.AddColumn<int>(
                name: "CechaUlicyId",
                table: "Ulice",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Ulice_CechaUlicyId",
                table: "Ulice",
                column: "CechaUlicyId");

            migrationBuilder.AddForeignKey(
                name: "FK_Ulice_CechyUlic_CechaUlicyId",
                table: "Ulice",
                column: "CechaUlicyId",
                principalTable: "CechyUlic",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Ulice_CechyUlic_CechaUlicyId",
                table: "Ulice");

            migrationBuilder.DropIndex(
                name: "IX_Ulice_CechaUlicyId",
                table: "Ulice");

            migrationBuilder.DropColumn(
                name: "CechaUlicyId",
                table: "Ulice");

            migrationBuilder.AddColumn<string>(
                name: "Cecha",
                table: "Ulice",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true,
                collation: "Polish_CS_AS");
        }
    }
}

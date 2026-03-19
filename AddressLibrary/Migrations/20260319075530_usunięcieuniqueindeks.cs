using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AddressLibrary.Migrations
{
    /// <inheritdoc />
    public partial class usunięcieuniqueindeks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TytulyStopnie_Skrot",
                table: "TytulyStopnie");

            migrationBuilder.CreateIndex(
                name: "IX_TytulyStopnie_Skrot",
                table: "TytulyStopnie",
                column: "Skrot");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TytulyStopnie_Skrot",
                table: "TytulyStopnie");

            migrationBuilder.CreateIndex(
                name: "IX_TytulyStopnie_Skrot",
                table: "TytulyStopnie",
                column: "Skrot",
                unique: true);
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AddressLibrary.Migrations
{
    /// <inheritdoc />
    public partial class DodanieTytulyStopnie : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TytulyStopnie",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nazwa = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Skrot = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Dopelniacz = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TytulyStopnie", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TytulyStopnie_Dopelniacz",
                table: "TytulyStopnie",
                column: "Dopelniacz");

            migrationBuilder.CreateIndex(
                name: "IX_TytulyStopnie_Nazwa",
                table: "TytulyStopnie",
                column: "Nazwa",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TytulyStopnie_Skrot",
                table: "TytulyStopnie",
                column: "Skrot",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TytulyStopnie");
        }
    }
}

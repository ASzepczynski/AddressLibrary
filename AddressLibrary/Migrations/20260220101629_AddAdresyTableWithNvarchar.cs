using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AddressLibrary.Migrations
{
    /// <inheritdoc />
    public partial class AddAdresyTableWithNvarchar : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Adresy",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    Kraj = table.Column<string>(type: "nvarchar(100)", nullable: true),
                    Kod = table.Column<string>(type: "nvarchar(10)", nullable: true),
                    Miasto = table.Column<string>(type: "nvarchar(200)", nullable: true),
                    Ulica = table.Column<string>(type: "nvarchar(200)", nullable: true),
                    NrDomu = table.Column<string>(type: "nvarchar(20)", nullable: true),
                    NrLokalu = table.Column<string>(type: "nvarchar(20)", nullable: true),
                    Wojewodztwo = table.Column<string>(type: "nvarchar(100)", nullable: true),
                    Powiat = table.Column<string>(type: "nvarchar(100)", nullable: true),
                    Gmina = table.Column<string>(type: "nvarchar(100)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Adresy", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Adresy_Kod",
                table: "Adresy",
                column: "Kod");

            migrationBuilder.CreateIndex(
                name: "IX_Adresy_Miasto_Kod",
                table: "Adresy",
                columns: new[] { "Miasto", "Kod" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Adresy");
        }
    }
}

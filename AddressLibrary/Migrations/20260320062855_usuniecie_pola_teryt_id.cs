using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AddressLibrary.Migrations
{
    /// <inheritdoc />
    public partial class usuniecie_pola_teryt_id : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TypyUlic_TerytUlicSymbol",
                table: "TypyUlic");

            migrationBuilder.DropColumn(
                name: "TerytUlicSymbol",
                table: "TypyUlic");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TerytUlicSymbol",
                table: "TypyUlic",
                type: "nvarchar(450)",
                nullable: true,
                collation: "Polish_CS_AS");

            migrationBuilder.CreateIndex(
                name: "IX_TypyUlic_TerytUlicSymbol",
                table: "TypyUlic",
                column: "TerytUlicSymbol");
        }
    }
}

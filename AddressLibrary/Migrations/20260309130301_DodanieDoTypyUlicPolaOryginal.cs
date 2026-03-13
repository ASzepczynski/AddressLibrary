using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AddressLibrary.Migrations
{
    /// <inheritdoc />
    public partial class DodanieDoTerytUlicPoprawkiPolaOryginal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Tytul",
                table: "TerytUlicPoprawki",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                comment: "Tytuł osoby (np. dr., prof., płk.)",
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true,
                oldComment: "Tytuł osoby (np. doktora, profesora)");

            migrationBuilder.AlterColumn<string>(
                name: "Prefiks",
                table: "TerytUlicPoprawki",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true,
                comment: "Prefiks nazwy ulicy (np. płk., gen., ks., im., imienia)",
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldNullable: true,
                oldComment: "Prefiks nazwy ulicy");

            migrationBuilder.AddColumn<string>(
                name: "Original",
                table: "TerytUlicPoprawki",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                comment: "Oryginalna pełna nazwa ulicy: Cecha + Nazwa2 + Nazwa1");

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

            migrationBuilder.AlterColumn<string>(
                name: "Tytul",
                table: "TerytUlicPoprawki",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                comment: "Tytuł osoby (np. doktora, profesora)",
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true,
                oldComment: "Tytuł osoby (np. dr., prof., płk.)");

            migrationBuilder.AlterColumn<string>(
                name: "Prefiks",
                table: "TerytUlicPoprawki",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true,
                comment: "Prefiks nazwy ulicy",
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldNullable: true,
                oldComment: "Prefiks nazwy ulicy (np. płk., gen., ks., im., imienia)");
        }
    }
}

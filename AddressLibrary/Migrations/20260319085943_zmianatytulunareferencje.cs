using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AddressLibrary.Migrations
{
    /// <inheritdoc />
    public partial class zmianatytulunareferencje : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TypyUlic_Imie_Nazwisko",
                table: "TypyUlic");

            migrationBuilder.DropIndex(
                name: "IX_TypyUlic_Nazwisko",
                table: "TypyUlic");

            migrationBuilder.DropColumn(
                name: "Tytul",
                table: "TypyUlic");

            migrationBuilder.AddColumn<string>(
                name: "TerytUlicSymbol",
                table: "TypyUlic",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true,
                comment: "Symbol ulicy z TERYT");

            migrationBuilder.AddColumn<int>(
                name: "TytulStopienId",
                table: "TypyUlic",
                type: "int",
                nullable: false,
                defaultValue: 0,
                comment: "Klucz obcy do tabeli TytulyStopnie (tytuł osoby, np. dr., prof., płk.)");

            migrationBuilder.CreateIndex(
                name: "IX_TypyUlic_TerytUlicSymbol",
                table: "TypyUlic",
                column: "TerytUlicSymbol");

            migrationBuilder.CreateIndex(
                name: "IX_TypyUlic_TytulStopienId",
                table: "TypyUlic",
                column: "TytulStopienId");

            migrationBuilder.AddForeignKey(
                name: "FK_TypyUlic_TytulyStopnie_TytulStopienId",
                table: "TypyUlic",
                column: "TytulStopienId",
                principalTable: "TytulyStopnie",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TypyUlic_TytulyStopnie_TytulStopienId",
                table: "TypyUlic");

            migrationBuilder.DropIndex(
                name: "IX_TypyUlic_TerytUlicSymbol",
                table: "TypyUlic");

            migrationBuilder.DropIndex(
                name: "IX_TypyUlic_TytulStopienId",
                table: "TypyUlic");

            migrationBuilder.DropColumn(
                name: "TerytUlicSymbol",
                table: "TypyUlic");

            migrationBuilder.DropColumn(
                name: "TytulStopienId",
                table: "TypyUlic");

            migrationBuilder.AddColumn<string>(
                name: "Tytul",
                table: "TypyUlic",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                comment: "Tytuł osoby (np. dr., prof., płk.)");

            migrationBuilder.CreateIndex(
                name: "IX_TypyUlic_Imie_Nazwisko",
                table: "TypyUlic",
                columns: new[] { "Imie", "Nazwisko" });

            migrationBuilder.CreateIndex(
                name: "IX_TypyUlic_Nazwisko",
                table: "TypyUlic",
                column: "Nazwisko");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AddressLibrary.Migrations
{
    /// <inheritdoc />
    public partial class ZmianaTytuluNaKluczObcy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Dodaj now¹ kolumnê TytulStopienId
            migrationBuilder.AddColumn<int>(
                name: "TytulStopienId",
                table: "TypyUlic",
                type: "int",
                nullable: false,
                defaultValue: -1);

            // Mapuj istniej¹ce wartoœci Tytul na TytulStopienId
            // Najpierw spróbuj dopasowaæ istniej¹ce tytu³y do s³ownika
            migrationBuilder.Sql(@"
                UPDATE tu
                SET tu.TytulStopienId = ISNULL(ts.Id, -1)
                FROM TypyUlic tu
                LEFT JOIN TytulyStopnie ts ON tu.Tytul = ts.Skrot
            ");

            // Usuñ star¹ kolumnê Tytul
            migrationBuilder.DropColumn(
                name: "Tytul",
                table: "TypyUlic");

            // Dodaj klucz obcy
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
            // Usuñ klucz obcy
            migrationBuilder.DropForeignKey(
                name: "FK_TypyUlic_TytulyStopnie_TytulStopienId",
                table: "TypyUlic");

            migrationBuilder.DropIndex(
                name: "IX_TypyUlic_TytulStopienId",
                table: "TypyUlic");

            // Dodaj z powrotem kolumnê Tytul
            migrationBuilder.AddColumn<string>(
                name: "Tytul",
                table: "TypyUlic",
                type: "nvarchar(max)",
                nullable: true);

            // Przywróæ wartoœci Tytul ze s³ownika
            migrationBuilder.Sql(@"
                UPDATE tu
                SET tu.Tytul = ts.Skrot
                FROM TypyUlic tu
                LEFT JOIN TytulyStopnie ts ON tu.TytulStopienId = ts.Id
            ");

            // Usuñ kolumnê TytulStopienId
            migrationBuilder.DropColumn(
                name: "TytulStopienId",
                table: "TypyUlic");
        }
    }
}
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AddressLibrary.Migrations
{
    /// <inheritdoc />
    public partial class zaczynamy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Miejscowosci_RodzajeMiejscowosci_RodzajMiejscowosciId",
                table: "Miejscowosci");

            migrationBuilder.DropIndex(
                name: "IX_Ulice_Symbol",
                table: "Ulice");

            migrationBuilder.AlterColumn<string>(
                name: "Nazwa2",
                table: "Ulice",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Nazwa1",
                table: "Ulice",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Cecha",
                table: "Ulice",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(10)",
                oldMaxLength: 10);

            migrationBuilder.AlterColumn<int>(
                name: "RodzajMiejscowosciId",
                table: "Miejscowosci",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Nazwa",
                table: "Miejscowosci",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Numery",
                table: "KodyPocztowe",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500);

            migrationBuilder.CreateIndex(
                name: "IX_Wojewodztwa_Nazwa",
                table: "Wojewodztwa",
                column: "Nazwa");

            migrationBuilder.CreateIndex(
                name: "IX_Ulice_Nazwa1",
                table: "Ulice",
                column: "Nazwa1");

            migrationBuilder.CreateIndex(
                name: "IX_Ulice_Symbol",
                table: "Ulice",
                column: "Symbol",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Powiaty_Nazwa",
                table: "Powiaty",
                column: "Nazwa");

            migrationBuilder.CreateIndex(
                name: "IX_Miejscowosci_Nazwa",
                table: "Miejscowosci",
                column: "Nazwa");

            migrationBuilder.CreateIndex(
                name: "IX_Gminy_Nazwa",
                table: "Gminy",
                column: "Nazwa");

            migrationBuilder.AddForeignKey(
                name: "FK_Miejscowosci_RodzajeMiejscowosci_RodzajMiejscowosciId",
                table: "Miejscowosci",
                column: "RodzajMiejscowosciId",
                principalTable: "RodzajeMiejscowosci",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Miejscowosci_RodzajeMiejscowosci_RodzajMiejscowosciId",
                table: "Miejscowosci");

            migrationBuilder.DropIndex(
                name: "IX_Wojewodztwa_Nazwa",
                table: "Wojewodztwa");

            migrationBuilder.DropIndex(
                name: "IX_Ulice_Nazwa1",
                table: "Ulice");

            migrationBuilder.DropIndex(
                name: "IX_Ulice_Symbol",
                table: "Ulice");

            migrationBuilder.DropIndex(
                name: "IX_Powiaty_Nazwa",
                table: "Powiaty");

            migrationBuilder.DropIndex(
                name: "IX_Miejscowosci_Nazwa",
                table: "Miejscowosci");

            migrationBuilder.DropIndex(
                name: "IX_Gminy_Nazwa",
                table: "Gminy");

            migrationBuilder.AlterColumn<string>(
                name: "Nazwa2",
                table: "Ulice",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Nazwa1",
                table: "Ulice",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "Cecha",
                table: "Ulice",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(10)",
                oldMaxLength: 10,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "RodzajMiejscowosciId",
                table: "Miejscowosci",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "Nazwa",
                table: "Miejscowosci",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "Numery",
                table: "KodyPocztowe",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000);

            migrationBuilder.CreateIndex(
                name: "IX_Ulice_Symbol",
                table: "Ulice",
                column: "Symbol");

            migrationBuilder.AddForeignKey(
                name: "FK_Miejscowosci_RodzajeMiejscowosci_RodzajMiejscowosciId",
                table: "Miejscowosci",
                column: "RodzajMiejscowosciId",
                principalTable: "RodzajeMiejscowosci",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}

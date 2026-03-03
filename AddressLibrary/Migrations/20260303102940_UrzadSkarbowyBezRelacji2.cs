using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AddressLibrary.Migrations
{
    /// <inheritdoc />
    public partial class UrzadSkarbowyBezRelacji2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UrzedySkarbowe_Ulice_UlicaId",
                table: "UrzedySkarbowe");

            migrationBuilder.DropIndex(
                name: "IX_UrzedySkarbowe_Email",
                table: "UrzedySkarbowe");

            migrationBuilder.DropIndex(
                name: "IX_UrzedySkarbowe_UlicaId",
                table: "UrzedySkarbowe");

            migrationBuilder.DropColumn(
                name: "UlicaId",
                table: "UrzedySkarbowe");

            migrationBuilder.AlterColumn<string>(
                name: "Www",
                table: "UrzedySkarbowe",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)");

            migrationBuilder.AlterColumn<string>(
                name: "NrDomu",
                table: "UrzedySkarbowe",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)");

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "UrzedySkarbowe",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)");

            migrationBuilder.AddColumn<string>(
                name: "Kod",
                table: "UrzedySkarbowe",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Miasto",
                table: "UrzedySkarbowe",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Ulica",
                table: "UrzedySkarbowe",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_UrzedySkarbowe_Kod",
                table: "UrzedySkarbowe",
                column: "Kod");

            migrationBuilder.CreateIndex(
                name: "IX_UrzedySkarbowe_Miasto",
                table: "UrzedySkarbowe",
                column: "Miasto");

            migrationBuilder.CreateIndex(
                name: "IX_UrzedySkarbowe_Miasto_Ulica",
                table: "UrzedySkarbowe",
                columns: new[] { "Miasto", "Ulica" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UrzedySkarbowe_Kod",
                table: "UrzedySkarbowe");

            migrationBuilder.DropIndex(
                name: "IX_UrzedySkarbowe_Miasto",
                table: "UrzedySkarbowe");

            migrationBuilder.DropIndex(
                name: "IX_UrzedySkarbowe_Miasto_Ulica",
                table: "UrzedySkarbowe");

            migrationBuilder.DropColumn(
                name: "Kod",
                table: "UrzedySkarbowe");

            migrationBuilder.DropColumn(
                name: "Miasto",
                table: "UrzedySkarbowe");

            migrationBuilder.DropColumn(
                name: "Ulica",
                table: "UrzedySkarbowe");

            migrationBuilder.AlterColumn<string>(
                name: "Www",
                table: "UrzedySkarbowe",
                type: "nvarchar(200)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "NrDomu",
                table: "UrzedySkarbowe",
                type: "nvarchar(20)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "UrzedySkarbowe",
                type: "nvarchar(100)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UlicaId",
                table: "UrzedySkarbowe",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_UrzedySkarbowe_Email",
                table: "UrzedySkarbowe",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_UrzedySkarbowe_UlicaId",
                table: "UrzedySkarbowe",
                column: "UlicaId");

            migrationBuilder.AddForeignKey(
                name: "FK_UrzedySkarbowe_Ulice_UlicaId",
                table: "UrzedySkarbowe",
                column: "UlicaId",
                principalTable: "Ulice",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}

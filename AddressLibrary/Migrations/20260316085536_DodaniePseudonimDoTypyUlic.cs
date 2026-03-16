using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AddressLibrary.Migrations
{
    /// <inheritdoc />
    public partial class DodaniePseudonimDoTypyUlic : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Tytul",
                table: "TypyUlic",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                comment: "Tytuł osoby (np. dr., prof., płk.)",
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true,
                oldComment: "Tytuł osoby (np. dr., prof., płk.)");

            migrationBuilder.AlterColumn<string>(
                name: "Prefiks",
                table: "TypyUlic",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                comment: "Prefiks nazwy ulicy (im., Leśny, Miejski)",
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 20,
                oldNullable: true,
                oldComment: "Prefiks nazwy ulicy (np. im., Leśny, Miejski)");

            migrationBuilder.AlterColumn<string>(
                name: "Postfiks",
                table: "TypyUlic",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                comment: "Postfiks/przydomek (dodatkowe informacje)",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true,
                oldComment: "Postfiks/przydomek (np. Zapory w Hieronima Dekutowskiego Zapory, Zośki w Tadeusza Zawadzkiego Zośki)");

            migrationBuilder.AlterColumn<string>(
                name: "Nazwisko2",
                table: "TypyUlic",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                comment: "Drugie nazwisko patrona ulicy (np. Reymonta w Władysława Stanisława Reymonta)",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true,
                oldComment: "Drugie nazwisko patrona ulicy (np. Reymonta w Władysława Stanisława Reymonta)");

            migrationBuilder.AlterColumn<string>(
                name: "Nazwisko",
                table: "TypyUlic",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                comment: "Pierwsze nazwisko patrona ulicy",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true,
                oldComment: "Pierwsze nazwisko patrona ulicy");

            migrationBuilder.AlterColumn<string>(
                name: "Imie2",
                table: "TypyUlic",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                comment: "Drugie imię patrona ulicy (np. Kamil w Krzysztofa Kamila Baczyńskiego)",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true,
                oldComment: "Drugie imię patrona ulicy (np. Kamil w Krzysztofa Kamila Baczyńskiego)");

            migrationBuilder.AlterColumn<string>(
                name: "Imie",
                table: "TypyUlic",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                comment: "Pierwsze imię patrona ulicy",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true,
                oldComment: "Pierwsze imię patrona ulicy");

            migrationBuilder.AddColumn<string>(
                name: "Pseudonim",
                table: "TypyUlic",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                comment: "Pseudonim patrona ulicy (np. Zapory, Zośki, Nila)");

            migrationBuilder.AddColumn<string>(
                name: "Pseudonim",
                table: "TerytUlicPoprawki",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Pseudonim",
                table: "TypyUlic");

            migrationBuilder.DropColumn(
                name: "Pseudonim",
                table: "TerytUlicPoprawki");

            migrationBuilder.AlterColumn<string>(
                name: "Tytul",
                table: "TypyUlic",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                comment: "Tytuł osoby (np. dr., prof., płk.)",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true,
                oldComment: "Tytuł osoby (np. dr., prof., płk.)");

            migrationBuilder.AlterColumn<string>(
                name: "Prefiks",
                table: "TypyUlic",
                type: "nvarchar(50)",
                maxLength: 20,
                nullable: true,
                comment: "Prefiks nazwy ulicy (np. im., Leśny, Miejski)",
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true,
                oldComment: "Prefiks nazwy ulicy (im., Leśny, Miejski)");

            migrationBuilder.AlterColumn<string>(
                name: "Postfiks",
                table: "TypyUlic",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                comment: "Postfiks/przydomek (np. Zapory w Hieronima Dekutowskiego Zapory, Zośki w Tadeusza Zawadzkiego Zośki)",
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true,
                oldComment: "Postfiks/przydomek (dodatkowe informacje)");

            migrationBuilder.AlterColumn<string>(
                name: "Nazwisko2",
                table: "TypyUlic",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                comment: "Drugie nazwisko patrona ulicy (np. Reymonta w Władysława Stanisława Reymonta)",
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true,
                oldComment: "Drugie nazwisko patrona ulicy (np. Reymonta w Władysława Stanisława Reymonta)");

            migrationBuilder.AlterColumn<string>(
                name: "Nazwisko",
                table: "TypyUlic",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                comment: "Pierwsze nazwisko patrona ulicy",
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true,
                oldComment: "Pierwsze nazwisko patrona ulicy");

            migrationBuilder.AlterColumn<string>(
                name: "Imie2",
                table: "TypyUlic",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                comment: "Drugie imię patrona ulicy (np. Kamil w Krzysztofa Kamila Baczyńskiego)",
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true,
                oldComment: "Drugie imię patrona ulicy (np. Kamil w Krzysztofa Kamila Baczyńskiego)");

            migrationBuilder.AlterColumn<string>(
                name: "Imie",
                table: "TypyUlic",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                comment: "Pierwsze imię patrona ulicy",
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true,
                oldComment: "Pierwsze imię patrona ulicy");
        }
    }
}

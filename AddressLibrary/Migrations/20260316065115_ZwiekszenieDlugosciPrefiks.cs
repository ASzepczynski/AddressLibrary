using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AddressLibrary.Migrations
{
    /// <inheritdoc />
    public partial class ZwiekszenieDlugosciPrefiks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Prefiks",
                table: "TypyUlic",
                type: "nvarchar(50)",
                maxLength: 20,
                nullable: true,
                comment: "Prefiks nazwy ulicy (np. im., Leśny, Miejski)",
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldNullable: true,
                oldComment: "Prefiks nazwy ulicy (np. płk., gen., ks., im.)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Prefiks",
                table: "TypyUlic",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true,
                comment: "Prefiks nazwy ulicy (np. płk., gen., ks., im.)",
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 20,
                oldNullable: true,
                oldComment: "Prefiks nazwy ulicy (np. im., Leśny, Miejski)");
        }
    }
}

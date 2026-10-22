using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddUserFormatSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Currency",
                schema: "invoice",
                table: "AppUsers",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "EUR");

            migrationBuilder.AddColumn<string>(
                name: "FormatCulture",
                schema: "invoice",
                table: "AppUsers",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TimeZone",
                schema: "invoice",
                table: "AppUsers",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "Europe/Tallinn");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Currency",
                schema: "invoice",
                table: "AppUsers");

            migrationBuilder.DropColumn(
                name: "FormatCulture",
                schema: "invoice",
                table: "AppUsers");

            migrationBuilder.DropColumn(
                name: "TimeZone",
                schema: "invoice",
                table: "AppUsers");
        }
    }
}

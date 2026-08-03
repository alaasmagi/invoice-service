using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddPublishedInvoiceEmailAttempt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PublishedInvoiceEmails_UserId_MonthlyStatementId",
                schema: "invoice",
                table: "PublishedInvoiceEmails");

            // Existing rows are first sends; backfill them as attempt 1 (their id uses the
            // suffix-free attempt-1 scheme). New inserts always set Attempt explicitly.
            migrationBuilder.AddColumn<int>(
                name: "Attempt",
                schema: "invoice",
                table: "PublishedInvoiceEmails",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateIndex(
                name: "IX_PublishedInvoiceEmails_UserId_MonthlyStatementId_Attempt",
                schema: "invoice",
                table: "PublishedInvoiceEmails",
                columns: new[] { "UserId", "MonthlyStatementId", "Attempt" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PublishedInvoiceEmails_UserId_MonthlyStatementId_Attempt",
                schema: "invoice",
                table: "PublishedInvoiceEmails");

            migrationBuilder.DropColumn(
                name: "Attempt",
                schema: "invoice",
                table: "PublishedInvoiceEmails");

            migrationBuilder.CreateIndex(
                name: "IX_PublishedInvoiceEmails_UserId_MonthlyStatementId",
                schema: "invoice",
                table: "PublishedInvoiceEmails",
                columns: new[] { "UserId", "MonthlyStatementId" },
                unique: true);
        }
    }
}

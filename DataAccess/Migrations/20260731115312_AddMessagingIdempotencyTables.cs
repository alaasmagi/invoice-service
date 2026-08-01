using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddMessagingIdempotencyTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProcessedInboxMessages",
                schema: "invoice",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EnvelopeId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Source = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Action = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessedInboxMessages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PublishedInvoiceEmails",
                schema: "invoice",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    MonthlyStatementId = table.Column<Guid>(type: "uuid", nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    Month = table.Column<int>(type: "integer", nullable: false),
                    InvoiceIdentifier = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Recipient = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    EnvelopeId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ConfirmedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PublishedInvoiceEmails", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProcessedInboxMessages_EnvelopeId",
                schema: "invoice",
                table: "ProcessedInboxMessages",
                column: "EnvelopeId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProcessedInboxMessages_ProcessedAt",
                schema: "invoice",
                table: "ProcessedInboxMessages",
                column: "ProcessedAt");

            migrationBuilder.CreateIndex(
                name: "IX_PublishedInvoiceEmails_EnvelopeId",
                schema: "invoice",
                table: "PublishedInvoiceEmails",
                column: "EnvelopeId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PublishedInvoiceEmails_UserId_MonthlyStatementId",
                schema: "invoice",
                table: "PublishedInvoiceEmails",
                columns: new[] { "UserId", "MonthlyStatementId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PublishedInvoiceEmails_UserId_Year_Month",
                schema: "invoice",
                table: "PublishedInvoiceEmails",
                columns: new[] { "UserId", "Year", "Month" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProcessedInboxMessages",
                schema: "invoice");

            migrationBuilder.DropTable(
                name: "PublishedInvoiceEmails",
                schema: "invoice");
        }
    }
}

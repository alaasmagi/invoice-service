using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddInvoiceDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "InvoiceDocumentId",
                schema: "invoice",
                table: "Invoices",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ApiKeys",
                schema: "invoice",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    KeyHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    KeyPrefix = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    LastUsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiKeys", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InvoiceDocuments",
                schema: "invoice",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StorageKey = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    FileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    Sha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    FailureReason = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    UploadedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ParseRequestedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ParseExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ServiceName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceDocuments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InvoiceDocumentRows",
                schema: "invoice",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InvoiceDocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false),
                    AddressText = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    InvoiceDate = table.Column<DateOnly>(type: "date", nullable: true),
                    PeriodStart = table.Column<DateOnly>(type: "date", nullable: true),
                    PeriodEnd = table.Column<DateOnly>(type: "date", nullable: true),
                    Outcome = table.Column<int>(type: "integer", nullable: false),
                    Reason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    InvoiceId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceDocumentRows", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InvoiceDocumentRows_InvoiceDocuments_InvoiceDocumentId",
                        column: x => x.InvoiceDocumentId,
                        principalSchema: "invoice",
                        principalTable: "InvoiceDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InvoiceDocumentRows_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalSchema: "invoice",
                        principalTable: "Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_InvoiceDocumentId",
                schema: "invoice",
                table: "Invoices",
                column: "InvoiceDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_ApiKeys_KeyHash",
                schema: "invoice",
                table: "ApiKeys",
                column: "KeyHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceDocumentRows_InvoiceDocumentId_Position",
                schema: "invoice",
                table: "InvoiceDocumentRows",
                columns: new[] { "InvoiceDocumentId", "Position" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceDocumentRows_InvoiceId",
                schema: "invoice",
                table: "InvoiceDocumentRows",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceDocuments_UserId_Sha256",
                schema: "invoice",
                table: "InvoiceDocuments",
                columns: new[] { "UserId", "Sha256" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceDocuments_UserId_UploadedAt",
                schema: "invoice",
                table: "InvoiceDocuments",
                columns: new[] { "UserId", "UploadedAt" });

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_InvoiceDocuments_InvoiceDocumentId",
                schema: "invoice",
                table: "Invoices",
                column: "InvoiceDocumentId",
                principalSchema: "invoice",
                principalTable: "InvoiceDocuments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_InvoiceDocuments_InvoiceDocumentId",
                schema: "invoice",
                table: "Invoices");

            migrationBuilder.DropTable(
                name: "ApiKeys",
                schema: "invoice");

            migrationBuilder.DropTable(
                name: "InvoiceDocumentRows",
                schema: "invoice");

            migrationBuilder.DropTable(
                name: "InvoiceDocuments",
                schema: "invoice");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_InvoiceDocumentId",
                schema: "invoice",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "InvoiceDocumentId",
                schema: "invoice",
                table: "Invoices");
        }
    }
}

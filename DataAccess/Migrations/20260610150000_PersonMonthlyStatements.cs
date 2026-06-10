using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    public partial class PersonMonthlyStatements : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM invoice.\"ContactMonthlyStatements\";");
            migrationBuilder.Sql("DELETE FROM invoice.\"MonthlyStatements\";");

            migrationBuilder.DropForeignKey(
                name: "FK_MonthlyStatements_Addresses_AddressId",
                schema: "invoice",
                table: "MonthlyStatements");

            migrationBuilder.DropIndex(
                name: "IX_MonthlyStatements_AddressId",
                schema: "invoice",
                table: "MonthlyStatements");

            migrationBuilder.DropColumn(
                name: "AddressId",
                schema: "invoice",
                table: "MonthlyStatements");

            migrationBuilder.AddColumn<Guid>(
                name: "ContactId",
                schema: "invoice",
                table: "MonthlyStatements",
                type: "uuid",
                nullable: false,
                defaultValue: Guid.Empty);

            migrationBuilder.CreateTable(
                name: "MonthlyStatementLines",
                schema: "invoice",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MonthlyStatementId = table.Column<Guid>(type: "uuid", nullable: false),
                    InvoiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    AddressId = table.Column<Guid>(type: "uuid", nullable: false),
                    AddressName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ServiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    InvoiceDate = table.Column<DateOnly>(type: "date", nullable: false),
                    PeriodStart = table.Column<DateOnly>(type: "date", nullable: true),
                    PeriodEnd = table.Column<DateOnly>(type: "date", nullable: true),
                    InvoiceTotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ResidentCount = table.Column<int>(type: "integer", nullable: false),
                    AllocatedAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ConcurrencyToken = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MonthlyStatementLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MonthlyStatementLines_Addresses_AddressId",
                        column: x => x.AddressId,
                        principalSchema: "invoice",
                        principalTable: "Addresses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MonthlyStatementLines_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalSchema: "invoice",
                        principalTable: "Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MonthlyStatementLines_MonthlyStatements_MonthlyStatementId",
                        column: x => x.MonthlyStatementId,
                        principalSchema: "invoice",
                        principalTable: "MonthlyStatements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MonthlyStatementLines_Services_ServiceId",
                        column: x => x.ServiceId,
                        principalSchema: "invoice",
                        principalTable: "Services",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MonthlyStatements_UserId_ContactId_Year_Month",
                schema: "invoice",
                table: "MonthlyStatements",
                columns: new[] { "UserId", "ContactId", "Year", "Month" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MonthlyStatements_ContactId",
                schema: "invoice",
                table: "MonthlyStatements",
                column: "ContactId");

            migrationBuilder.CreateIndex(
                name: "IX_MonthlyStatementLines_AddressId",
                schema: "invoice",
                table: "MonthlyStatementLines",
                column: "AddressId");

            migrationBuilder.CreateIndex(
                name: "IX_MonthlyStatementLines_InvoiceId",
                schema: "invoice",
                table: "MonthlyStatementLines",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_MonthlyStatementLines_MonthlyStatementId",
                schema: "invoice",
                table: "MonthlyStatementLines",
                column: "MonthlyStatementId");

            migrationBuilder.CreateIndex(
                name: "IX_MonthlyStatementLines_ServiceId",
                schema: "invoice",
                table: "MonthlyStatementLines",
                column: "ServiceId");

            migrationBuilder.AddForeignKey(
                name: "FK_MonthlyStatements_Contacts_ContactId",
                schema: "invoice",
                table: "MonthlyStatements",
                column: "ContactId",
                principalSchema: "invoice",
                principalTable: "Contacts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MonthlyStatementLines",
                schema: "invoice");

            migrationBuilder.DropForeignKey(
                name: "FK_MonthlyStatements_Contacts_ContactId",
                schema: "invoice",
                table: "MonthlyStatements");

            migrationBuilder.DropIndex(
                name: "IX_MonthlyStatements_UserId_ContactId_Year_Month",
                schema: "invoice",
                table: "MonthlyStatements");

            migrationBuilder.DropIndex(
                name: "IX_MonthlyStatements_ContactId",
                schema: "invoice",
                table: "MonthlyStatements");

            migrationBuilder.DropColumn(
                name: "ContactId",
                schema: "invoice",
                table: "MonthlyStatements");

            migrationBuilder.AddColumn<Guid>(
                name: "AddressId",
                schema: "invoice",
                table: "MonthlyStatements",
                type: "uuid",
                nullable: false,
                defaultValue: Guid.Empty);

            migrationBuilder.CreateIndex(
                name: "IX_MonthlyStatements_AddressId",
                schema: "invoice",
                table: "MonthlyStatements",
                column: "AddressId");

            migrationBuilder.AddForeignKey(
                name: "FK_MonthlyStatements_Addresses_AddressId",
                schema: "invoice",
                table: "MonthlyStatements",
                column: "AddressId",
                principalSchema: "invoice",
                principalTable: "Addresses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddAllocationRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AllocationMethod",
                schema: "invoice",
                table: "MonthlyStatementLines",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "AllocationRuleId",
                schema: "invoice",
                table: "MonthlyStatementLines",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ShareValue",
                schema: "invoice",
                table: "MonthlyStatementLines",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsOverride",
                schema: "invoice",
                table: "InvoiceAllocations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "AllocationRules",
                schema: "invoice",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AddressId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceId = table.Column<Guid>(type: "uuid", nullable: true),
                    Method = table.Column<int>(type: "integer", nullable: false),
                    ValidFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    ValidTo = table.Column<DateOnly>(type: "date", nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ConcurrencyToken = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AllocationRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AllocationRules_Addresses_AddressId",
                        column: x => x.AddressId,
                        principalSchema: "invoice",
                        principalTable: "Addresses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AllocationRules_Services_ServiceId",
                        column: x => x.ServiceId,
                        principalSchema: "invoice",
                        principalTable: "Services",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AllocationRuleShares",
                schema: "invoice",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AllocationRuleId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContactId = table.Column<Guid>(type: "uuid", nullable: false),
                    Value = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ConcurrencyToken = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AllocationRuleShares", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AllocationRuleShares_AllocationRules_AllocationRuleId",
                        column: x => x.AllocationRuleId,
                        principalSchema: "invoice",
                        principalTable: "AllocationRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AllocationRuleShares_Contacts_ContactId",
                        column: x => x.ContactId,
                        principalSchema: "invoice",
                        principalTable: "Contacts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MonthlyStatementLines_AllocationRuleId",
                schema: "invoice",
                table: "MonthlyStatementLines",
                column: "AllocationRuleId");

            migrationBuilder.CreateIndex(
                name: "IX_AllocationRules_AddressId",
                schema: "invoice",
                table: "AllocationRules",
                column: "AddressId");

            migrationBuilder.CreateIndex(
                name: "IX_AllocationRules_ServiceId",
                schema: "invoice",
                table: "AllocationRules",
                column: "ServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_AllocationRules_UserId_AddressId_ServiceId_ValidFrom",
                schema: "invoice",
                table: "AllocationRules",
                columns: new[] { "UserId", "AddressId", "ServiceId", "ValidFrom" });

            migrationBuilder.CreateIndex(
                name: "IX_AllocationRuleShares_AllocationRuleId_ContactId",
                schema: "invoice",
                table: "AllocationRuleShares",
                columns: new[] { "AllocationRuleId", "ContactId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AllocationRuleShares_ContactId",
                schema: "invoice",
                table: "AllocationRuleShares",
                column: "ContactId");

            migrationBuilder.AddForeignKey(
                name: "FK_MonthlyStatementLines_AllocationRules_AllocationRuleId",
                schema: "invoice",
                table: "MonthlyStatementLines",
                column: "AllocationRuleId",
                principalSchema: "invoice",
                principalTable: "AllocationRules",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MonthlyStatementLines_AllocationRules_AllocationRuleId",
                schema: "invoice",
                table: "MonthlyStatementLines");

            migrationBuilder.DropTable(
                name: "AllocationRuleShares",
                schema: "invoice");

            migrationBuilder.DropTable(
                name: "AllocationRules",
                schema: "invoice");

            migrationBuilder.DropIndex(
                name: "IX_MonthlyStatementLines_AllocationRuleId",
                schema: "invoice",
                table: "MonthlyStatementLines");

            migrationBuilder.DropColumn(
                name: "AllocationMethod",
                schema: "invoice",
                table: "MonthlyStatementLines");

            migrationBuilder.DropColumn(
                name: "AllocationRuleId",
                schema: "invoice",
                table: "MonthlyStatementLines");

            migrationBuilder.DropColumn(
                name: "ShareValue",
                schema: "invoice",
                table: "MonthlyStatementLines");

            migrationBuilder.DropColumn(
                name: "IsOverride",
                schema: "invoice",
                table: "InvoiceAllocations");
        }
    }
}

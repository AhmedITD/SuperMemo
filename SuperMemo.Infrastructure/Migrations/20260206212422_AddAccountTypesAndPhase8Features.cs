using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SuperMemo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountTypesAndPhase8Features : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FailureReason",
                table: "Transactions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RetryCount",
                table: "Transactions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "RiskLevel",
                table: "Transactions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RiskScore",
                table: "Transactions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StatusChangedAt",
                table: "Transactions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AccountType",
                table: "Accounts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "DailySpendingLimit",
                table: "Accounts",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DailySpentAmount",
                table: "Accounts",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastDailyLimitResetDate",
                table: "Accounts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastInterestCalculationDate",
                table: "Accounts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "FraudDetectionRules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RuleName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    RuleType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ThresholdValue = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    ThresholdCount = table.Column<int>(type: "integer", nullable: true),
                    TimeWindow = table.Column<TimeSpan>(type: "interval", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FraudDetectionRules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MerchantAccounts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AccountId = table.Column<int>(type: "integer", nullable: false),
                    MerchantId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    MerchantName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    QrCodeData = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    NfcUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MerchantAccounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MerchantAccounts_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TransactionStatusHistory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TransactionId = table.Column<int>(type: "integer", nullable: false),
                    OldStatus = table.Column<int>(type: "integer", nullable: false),
                    NewStatus = table.Column<int>(type: "integer", nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ChangedBy = table.Column<int>(type: "integer", nullable: true),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransactionStatusHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TransactionStatusHistory_Transactions_TransactionId",
                        column: x => x.TransactionId,
                        principalTable: "Transactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_RiskLevel_Status",
                table: "Transactions",
                columns: new[] { "RiskLevel", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_Status_StatusChangedAt",
                table: "Transactions",
                columns: new[] { "Status", "StatusChangedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_AccountType",
                table: "Accounts",
                column: "AccountType");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_LastDailyLimitResetDate",
                table: "Accounts",
                column: "LastDailyLimitResetDate");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_LastInterestCalculationDate",
                table: "Accounts",
                column: "LastInterestCalculationDate");

            migrationBuilder.CreateIndex(
                name: "IX_FraudDetectionRules_IsActive",
                table: "FraudDetectionRules",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_FraudDetectionRules_RuleType",
                table: "FraudDetectionRules",
                column: "RuleType");

            migrationBuilder.CreateIndex(
                name: "IX_MerchantAccounts_AccountId",
                table: "MerchantAccounts",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_MerchantAccounts_MerchantId",
                table: "MerchantAccounts",
                column: "MerchantId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TransactionStatusHistory_ChangedAt",
                table: "TransactionStatusHistory",
                column: "ChangedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionStatusHistory_TransactionId",
                table: "TransactionStatusHistory",
                column: "TransactionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FraudDetectionRules");

            migrationBuilder.DropTable(
                name: "MerchantAccounts");

            migrationBuilder.DropTable(
                name: "TransactionStatusHistory");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_RiskLevel_Status",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_Status_StatusChangedAt",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_Accounts_AccountType",
                table: "Accounts");

            migrationBuilder.DropIndex(
                name: "IX_Accounts_LastDailyLimitResetDate",
                table: "Accounts");

            migrationBuilder.DropIndex(
                name: "IX_Accounts_LastInterestCalculationDate",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "FailureReason",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "RetryCount",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "RiskLevel",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "RiskScore",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "StatusChangedAt",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "AccountType",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "DailySpendingLimit",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "DailySpentAmount",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "LastDailyLimitResetDate",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "LastInterestCalculationDate",
                table: "Accounts");
        }
    }
}

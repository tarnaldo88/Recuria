using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Recuria.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProcessedEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BillingAttempt_Subscriptions_SubscriptionId",
                table: "BillingAttempt");

            migrationBuilder.DropPrimaryKey(
                name: "PK_BillingAttempt",
                table: "BillingAttempt");

            migrationBuilder.RenameTable(
                name: "BillingAttempt",
                newName: "BillingAttempts");

            migrationBuilder.RenameIndex(
                name: "IX_BillingAttempt_SubscriptionId",
                table: "BillingAttempts",
                newName: "IX_BillingAttempts_SubscriptionId");

            migrationBuilder.AddColumn<int>(
                name: "AttemptCount",
                table: "OutBoxMessages",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "NextAttemptOnUtc",
                table: "OutBoxMessages",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_BillingAttempts",
                table: "BillingAttempts",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "ProcessedEvents",
                columns: table => new
                {
                    EventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProcessedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessedEvents", x => x.EventId);
                });

            migrationBuilder.AddForeignKey(
                name: "FK_BillingAttempts_Subscriptions_SubscriptionId",
                table: "BillingAttempts",
                column: "SubscriptionId",
                principalTable: "Subscriptions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BillingAttempts_Subscriptions_SubscriptionId",
                table: "BillingAttempts");

            migrationBuilder.DropTable(
                name: "ProcessedEvents");

            migrationBuilder.DropPrimaryKey(
                name: "PK_BillingAttempts",
                table: "BillingAttempts");

            migrationBuilder.DropColumn(
                name: "AttemptCount",
                table: "OutBoxMessages");

            migrationBuilder.DropColumn(
                name: "NextAttemptOnUtc",
                table: "OutBoxMessages");

            migrationBuilder.RenameTable(
                name: "BillingAttempts",
                newName: "BillingAttempt");

            migrationBuilder.RenameIndex(
                name: "IX_BillingAttempts_SubscriptionId",
                table: "BillingAttempt",
                newName: "IX_BillingAttempt_SubscriptionId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_BillingAttempt",
                table: "BillingAttempt",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_BillingAttempt_Subscriptions_SubscriptionId",
                table: "BillingAttempt",
                column: "SubscriptionId",
                principalTable: "Subscriptions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Recuria.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStripePaymentTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StripeSubscriptionMaps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StripeCustomerId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    StripeSubscriptionId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StripeSubscriptionMaps", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StripeWebhookEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StripeEventId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ReceivedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StripeWebhookEvents", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StripeSubscriptionMaps_OrganizationId",
                table: "StripeSubscriptionMaps",
                column: "OrganizationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StripeSubscriptionMaps_StripeCustomerId",
                table: "StripeSubscriptionMaps",
                column: "StripeCustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_StripeSubscriptionMaps_StripeSubscriptionId",
                table: "StripeSubscriptionMaps",
                column: "StripeSubscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_StripeWebhookEvents_StripeEventId",
                table: "StripeWebhookEvents",
                column: "StripeEventId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StripeSubscriptionMaps");

            migrationBuilder.DropTable(
                name: "StripeWebhookEvents");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Recuria.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixSubscriptionRelationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Subscriptions_Organizations_Organization_Id",
                table: "Subscriptions");

            migrationBuilder.DropIndex(
                name: "IX_Subscriptions_Organization_Id",
                table: "Subscriptions");

            migrationBuilder.DropIndex(
                name: "IX_Subscriptions_OrganizationId",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "Organization_Id",
                table: "Subscriptions");

            migrationBuilder.RenameColumn(
                name: "Plan_",
                table: "Subscriptions",
                newName: "Plan");

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_OrganizationId",
                table: "Subscriptions",
                column: "OrganizationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Subscriptions_OrganizationId",
                table: "Subscriptions");

            migrationBuilder.RenameColumn(
                name: "Plan",
                table: "Subscriptions",
                newName: "Plan_");

            migrationBuilder.AddColumn<Guid>(
                name: "Organization_Id",
                table: "Subscriptions",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_Organization_Id",
                table: "Subscriptions",
                column: "Organization_Id");

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_OrganizationId",
                table: "Subscriptions",
                column: "OrganizationId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Subscriptions_Organizations_Organization_Id",
                table: "Subscriptions",
                column: "Organization_Id",
                principalTable: "Organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

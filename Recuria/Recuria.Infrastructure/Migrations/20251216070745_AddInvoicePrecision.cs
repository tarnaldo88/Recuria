using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Recuria.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInvoicePrecision : Migration
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

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId1",
                table: "Subscriptions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_OrganizationId",
                table: "Subscriptions",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_OrganizationId1",
                table: "Subscriptions",
                column: "OrganizationId1",
                unique: true,
                filter: "[OrganizationId1] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Subscriptions_Organizations_OrganizationId1",
                table: "Subscriptions",
                column: "OrganizationId1",
                principalTable: "Organizations",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Subscriptions_Organizations_OrganizationId1",
                table: "Subscriptions");

            migrationBuilder.DropIndex(
                name: "IX_Subscriptions_OrganizationId",
                table: "Subscriptions");

            migrationBuilder.DropIndex(
                name: "IX_Subscriptions_OrganizationId1",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "OrganizationId1",
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

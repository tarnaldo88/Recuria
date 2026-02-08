using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Recuria.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInvoicePaidOnUtc : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "PaidOnUtc",
                table: "Invoices",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PaidOnUtc",
                table: "Invoices");
        }
    }
}

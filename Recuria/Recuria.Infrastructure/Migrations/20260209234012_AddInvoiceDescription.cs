using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Recuria.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInvoiceDescription : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Invoices",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                table: "Invoices");
        }
    }
}

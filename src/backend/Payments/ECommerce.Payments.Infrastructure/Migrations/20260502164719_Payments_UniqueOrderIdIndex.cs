using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.Payments.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Payments_UniqueOrderIdIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Payments_OrderId",
                schema: "payments",
                table: "Payments");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_OrderId",
                schema: "payments",
                table: "Payments",
                column: "OrderId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Payments_OrderId",
                schema: "payments",
                table: "Payments");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_OrderId",
                schema: "payments",
                table: "Payments",
                column: "OrderId");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserRowVersionAndDiscountTypeEnum : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Users",
                type: "bytea",
                rowVersion: true,
                nullable: true);

            // Convert existing string values to integer before altering column type
            // Percentage = 0, Fixed = 1 (matches enum ordinal values)
            migrationBuilder.Sql(@"
                UPDATE ""PromoCodes""
                SET ""DiscountType"" = CASE LOWER(""DiscountType"")
                    WHEN 'percentage' THEN '0'
                    WHEN 'fixed'      THEN '1'
                    ELSE '0'
                END");

            migrationBuilder.AlterColumn<int>(
                name: "DiscountType",
                table: "PromoCodes",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Users");

            migrationBuilder.AlterColumn<string>(
                name: "DiscountType",
                table: "PromoCodes",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");
        }
    }
}

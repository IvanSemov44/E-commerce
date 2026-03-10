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

            // Convert text values to enum ordinals as part of the type change.
            // Handles legacy labels (percentage/fixed) and numeric strings.
            migrationBuilder.Sql(@"
                ALTER TABLE ""PromoCodes""
                ALTER COLUMN ""DiscountType"" TYPE integer
                USING CASE
                    WHEN ""DiscountType"" ~ '^[0-9]+$' THEN ""DiscountType""::integer
                    WHEN LOWER(""DiscountType"") = 'percentage' THEN 0
                    WHEN LOWER(""DiscountType"") = 'fixed' THEN 1
                    ELSE 0
                END;");
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

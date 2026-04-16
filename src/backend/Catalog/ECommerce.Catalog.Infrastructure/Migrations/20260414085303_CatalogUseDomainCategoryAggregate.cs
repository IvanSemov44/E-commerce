using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.Catalog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CatalogUseDomainCategoryAggregate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                schema: "catalog",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "ImageUrl",
                schema: "catalog",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "SortOrder",
                schema: "catalog",
                table: "Categories");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_IsActive",
                schema: "catalog",
                table: "Categories",
                column: "IsActive");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Categories_IsActive",
                schema: "catalog",
                table: "Categories");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                schema: "catalog",
                table: "Categories",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                schema: "catalog",
                table: "Categories",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SortOrder",
                schema: "catalog",
                table: "Categories",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}

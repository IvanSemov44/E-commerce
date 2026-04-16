using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.Catalog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CatalogPersistProductStatusAndCategoryFk : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Products_Categories_CategoryId",
                schema: "catalog",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_IsActive",
                schema: "catalog",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_IsActive_Price",
                schema: "catalog",
                table: "Products");

            migrationBuilder.AddColumn<string>(
                name: "Status",
                schema: "catalog",
                table: "Products",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Inactive");

            migrationBuilder.Sql(
                "UPDATE catalog.\"Products\" SET \"Status\" = CASE WHEN \"IsActive\" THEN 'Active' ELSE 'Inactive' END;");

            migrationBuilder.DropColumn(
                name: "IsActive",
                schema: "catalog",
                table: "Products");

            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM catalog."Products" WHERE "CategoryId" IS NULL) THEN
                        RAISE EXCEPTION 'Cannot enforce non-null CategoryId for catalog.Products because NULL values exist.';
                    END IF;
                END $$;
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "CategoryId",
                schema: "catalog",
                table: "Products",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Products_Status_Price",
                schema: "catalog",
                table: "Products",
                columns: new[] { "Status", "Price" });

            migrationBuilder.AddForeignKey(
                name: "FK_Products_Categories_CategoryId",
                schema: "catalog",
                table: "Products",
                column: "CategoryId",
                principalSchema: "catalog",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Products_Categories_CategoryId",
                schema: "catalog",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_Status_Price",
                schema: "catalog",
                table: "Products");

            migrationBuilder.AlterColumn<Guid>(
                name: "CategoryId",
                schema: "catalog",
                table: "Products",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                schema: "catalog",
                table: "Products",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql(
                "UPDATE catalog.\"Products\" SET \"IsActive\" = CASE WHEN \"Status\" = 'Active' THEN TRUE ELSE FALSE END;");

            migrationBuilder.DropColumn(
                name: "Status",
                schema: "catalog",
                table: "Products");

            migrationBuilder.CreateIndex(
                name: "IX_Products_IsActive",
                schema: "catalog",
                table: "Products",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Products_IsActive_Price",
                schema: "catalog",
                table: "Products",
                columns: new[] { "IsActive", "Price" });

            migrationBuilder.AddForeignKey(
                name: "FK_Products_Categories_CategoryId",
                schema: "catalog",
                table: "Products",
                column: "CategoryId",
                principalSchema: "catalog",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}

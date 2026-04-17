using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.Reviews.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Reviews_SchemaAndSoftDelete_Consolidated : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "Reviews",
                newName: "Reviews",
                newSchema: "reviews");

            migrationBuilder.RenameTable(
                name: "ReviewProductProjections",
                newName: "ReviewProductProjections",
                newSchema: "reviews");

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                schema: "reviews",
                table: "Reviews",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                schema: "reviews",
                table: "Reviews");

            migrationBuilder.RenameTable(
                name: "Reviews",
                schema: "reviews",
                newName: "Reviews");

            migrationBuilder.RenameTable(
                name: "ReviewProductProjections",
                schema: "reviews",
                newName: "ReviewProductProjections");
        }
    }
}

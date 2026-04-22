using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.Identity.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Identity_AlignAddressColumnNamesToDomain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "StreetLine1",
                schema: "identity",
                table: "Addresses",
                newName: "Street");

            migrationBuilder.RenameColumn(
                name: "IsDefault",
                schema: "identity",
                table: "Addresses",
                newName: "IsDefaultShipping");

            migrationBuilder.AlterColumn<bool>(
                name: "IsDefaultShipping",
                schema: "identity",
                table: "Addresses",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "boolean");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Street",
                schema: "identity",
                table: "Addresses",
                newName: "StreetLine1");

            migrationBuilder.RenameColumn(
                name: "IsDefaultShipping",
                schema: "identity",
                table: "Addresses",
                newName: "IsDefault");

            migrationBuilder.AlterColumn<bool>(
                name: "IsDefault",
                schema: "identity",
                table: "Addresses",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: false);
        }
    }
}

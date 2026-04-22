using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.Identity.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Identity_AlignColumnNamesToDomain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FirstName",
                schema: "identity",
                table: "Addresses");

            migrationBuilder.DropColumn(
                name: "LastName",
                schema: "identity",
                table: "Addresses");

            migrationBuilder.DropColumn(
                name: "State",
                schema: "identity",
                table: "Addresses");

            migrationBuilder.DropColumn(
                name: "Type",
                schema: "identity",
                table: "Addresses");

            migrationBuilder.RenameColumn(
                name: "Phone",
                schema: "identity",
                table: "Users",
                newName: "PhoneNumber");

            migrationBuilder.RenameColumn(
                name: "PasswordResetExpires",
                schema: "identity",
                table: "Users",
                newName: "PasswordResetExpiry");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PhoneNumber",
                schema: "identity",
                table: "Users",
                newName: "Phone");

            migrationBuilder.RenameColumn(
                name: "PasswordResetExpiry",
                schema: "identity",
                table: "Users",
                newName: "PasswordResetExpires");

            migrationBuilder.AddColumn<string>(
                name: "FirstName",
                schema: "identity",
                table: "Addresses",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastName",
                schema: "identity",
                table: "Addresses",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "State",
                schema: "identity",
                table: "Addresses",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Type",
                schema: "identity",
                table: "Addresses",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);
        }
    }
}

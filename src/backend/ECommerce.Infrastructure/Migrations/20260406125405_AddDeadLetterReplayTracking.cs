using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDeadLetterReplayTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "RequeuedAt",
                schema: "integration",
                table: "dead_letter_messages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_dead_letter_messages_RequeuedAt",
                schema: "integration",
                table: "dead_letter_messages",
                column: "RequeuedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_dead_letter_messages_RequeuedAt",
                schema: "integration",
                table: "dead_letter_messages");

            migrationBuilder.DropColumn(
                name: "RequeuedAt",
                schema: "integration",
                table: "dead_letter_messages");
        }
    }
}

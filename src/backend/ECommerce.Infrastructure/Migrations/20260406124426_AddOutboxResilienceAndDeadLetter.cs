using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOutboxResilienceAndDeadLetter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeadLetteredAt",
                schema: "integration",
                table: "outbox_messages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeadLettered",
                schema: "integration",
                table: "outbox_messages",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "NextAttemptAt",
                schema: "integration",
                table: "outbox_messages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "dead_letter_messages",
                schema: "integration",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OutboxMessageId = table.Column<Guid>(type: "uuid", nullable: false),
                    IdempotencyKey = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    EventData = table.Column<string>(type: "text", nullable: false),
                    RetryCount = table.Column<int>(type: "integer", nullable: false),
                    LastError = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    FailedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dead_letter_messages", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_IsDeadLettered",
                schema: "integration",
                table: "outbox_messages",
                column: "IsDeadLettered");

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_NextAttemptAt",
                schema: "integration",
                table: "outbox_messages",
                column: "NextAttemptAt");

            migrationBuilder.CreateIndex(
                name: "IX_dead_letter_messages_FailedAt",
                schema: "integration",
                table: "dead_letter_messages",
                column: "FailedAt");

            migrationBuilder.CreateIndex(
                name: "IX_dead_letter_messages_OutboxMessageId",
                schema: "integration",
                table: "dead_letter_messages",
                column: "OutboxMessageId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "dead_letter_messages",
                schema: "integration");

            migrationBuilder.DropIndex(
                name: "IX_outbox_messages_IsDeadLettered",
                schema: "integration",
                table: "outbox_messages");

            migrationBuilder.DropIndex(
                name: "IX_outbox_messages_NextAttemptAt",
                schema: "integration",
                table: "outbox_messages");

            migrationBuilder.DropColumn(
                name: "DeadLetteredAt",
                schema: "integration",
                table: "outbox_messages");

            migrationBuilder.DropColumn(
                name: "IsDeadLettered",
                schema: "integration",
                table: "outbox_messages");

            migrationBuilder.DropColumn(
                name: "NextAttemptAt",
                schema: "integration",
                table: "outbox_messages");
        }
    }
}

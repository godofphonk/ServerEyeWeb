using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServerEye.Infrastructure.Migrations.BillingDb
{
    /// <inheritdoc />
    public partial class FixWebhookEventSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create OutboxMessages table
            migrationBuilder.CreateTable(
                name: "OutboxMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MessageType = table.Column<string>(type: "text", nullable: false),
                    Payload = table.Column<string>(type: "jsonb", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    RetryCount = table.Column<int>(type: "integer", nullable: false),
                    Error = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxMessages", x => x.Id);
                });

            // Rename Payload to RawPayload
            migrationBuilder.RenameColumn(
                name: "Payload",
                table: "WebhookEvents",
                newName: "RawPayload");

            // Remove IsProcessed
            migrationBuilder.DropColumn(
                name: "IsProcessed",
                table: "WebhookEvents");

            // Add Status column
            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "WebhookEvents",
                type: "text",
                nullable: false,
                defaultValue: "Received");

            // Add Headers column
            migrationBuilder.AddColumn<string>(
                name: "Headers",
                table: "WebhookEvents",
                type: "text",
                nullable: true);

            // Add UpdatedAt column
            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "WebhookEvents",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc));

            // Create index on Status
            migrationBuilder.CreateIndex(
                name: "IX_WebhookEvents_Status",
                table: "WebhookEvents",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WebhookEvents_Status",
                table: "WebhookEvents");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "WebhookEvents");

            migrationBuilder.DropColumn(
                name: "Headers",
                table: "WebhookEvents");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "WebhookEvents");

            migrationBuilder.AddColumn<bool>(
                name: "IsProcessed",
                table: "WebhookEvents",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.RenameColumn(
                name: "RawPayload",
                table: "WebhookEvents",
                newName: "Payload");

            // Drop OutboxMessages table
            migrationBuilder.DropTable(
                name: "OutboxMessages");
        }
    }
}

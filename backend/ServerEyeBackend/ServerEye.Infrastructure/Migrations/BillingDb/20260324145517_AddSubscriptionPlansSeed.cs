using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServerEye.Infrastructure.Migrations.BillingDb
{
    /// <inheritdoc />
    public partial class AddSubscriptionPlansSeed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var now = new DateTime(2026, 3, 24, 14, 55, 0, DateTimeKind.Utc);
            
            // Seed Free plan
            migrationBuilder.InsertData(
                table: "subscriptionplans",
                columns: new[] { "id", "name", "description", "price", "features", "isactive", "createdat", "updatedat" },
                values: new object[] { Guid.Parse("f5e8c3a1-2b4d-4e6f-8a9c-1d2e3f4a5b6c"), "Free", "Basic monitoring for single server", 0.00m, new[] { "Up to 1 servers", "7-day data retention", "1 server monitoring", "7 days retention" }, true, now, now });

            // Seed Pro plan
            migrationBuilder.InsertData(
                table: "subscriptionplans",
                columns: new[] { "id", "name", "description", "price", "features", "isactive", "createdat", "updatedat" },
                values: new object[] { Guid.Parse("841bb3db-424c-46e5-a752-04641391c993"), "Pro", "Advanced monitoring for multiple servers", 9.99m, new[] { "Up to 10 servers", "30-day data retention", "Custom alerts", "API access", "10 servers", "30 days retention", "Real-time alerts", "API access" }, true, now, now });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove seed data
            migrationBuilder.DeleteData(
                table: "subscriptionplans",
                keyColumn: "id",
                keyValues: new object[]
                {
                    Guid.Parse("f5e8c3a1-2b4d-4e6f-8a9c-1d2e3f4a5b6c"),
                    Guid.Parse("841bb3db-424c-46e5-a752-04641391c993")
                });
        }
    }
}

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
            
            // Seed Pro plan
            migrationBuilder.InsertData(
                table: "subscriptionplans",
                columns: new[] { "id", "name", "description", "price", "features", "isactive", "createdat", "updatedat" },
                values: new object[] { Guid.Parse("841bb3db-424c-46e5-a752-04641391c993"), "Pro", "Professional plan with advanced features", 9.99m, new[] { "Unlimited servers", "Advanced monitoring", "Priority support", "API access" }, true, now, now });

            // Seed Basic plan
            migrationBuilder.InsertData(
                table: "subscriptionplans",
                columns: new[] { "id", "name", "description", "price", "features", "isactive", "createdat", "updatedat" },
                values: new object[] { Guid.Parse("f5e8c3a1-2b4d-4e6f-8a9c-1d2e3f4a5b6c"), "Basic", "Basic plan for small projects", 4.99m, new[] { "Up to 5 servers", "Basic monitoring", "Email support" }, true, now, now });

            // Seed Enterprise plan
            migrationBuilder.InsertData(
                table: "subscriptionplans",
                columns: new[] { "id", "name", "description", "price", "features", "isactive", "createdat", "updatedat" },
                values: new object[] { Guid.Parse("a1b2c3d4-e5f6-4a5b-8c9d-0e1f2a3b4c5d"), "Enterprise", "Enterprise plan with all features", 29.99m, new[] { "Unlimited servers", "Advanced monitoring", "24/7 support", "API access", "Custom integrations", "SLA guarantee" }, true, now, now });
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
                    Guid.Parse("841bb3db-424c-46e5-a752-04641391c993"),
                    Guid.Parse("f5e8c3a1-2b4d-4e6f-8a9c-1d2e3f4a5b6c"),
                    Guid.Parse("a1b2c3d4-e5f6-4a5b-8c9d-0e1f2a3b4c5d")
                });
        }
    }
}

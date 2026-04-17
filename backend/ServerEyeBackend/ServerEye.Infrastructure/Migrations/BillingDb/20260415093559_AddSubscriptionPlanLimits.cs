using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServerEye.Infrastructure.Migrations.BillingDb
{
    /// <inheritdoc />
    public partial class AddSubscriptionPlanLimits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "hasalerts",
                table: "subscriptionplans",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "hasapiaccess",
                table: "subscriptionplans",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "hasprioritysupport",
                table: "subscriptionplans",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "maxservers",
                table: "subscriptionplans",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "metricsretentiondays",
                table: "subscriptionplans",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "monthlyprice",
                table: "subscriptionplans",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "yearlyprice",
                table: "subscriptionplans",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "hasalerts",
                table: "subscriptionplans");

            migrationBuilder.DropColumn(
                name: "hasapiaccess",
                table: "subscriptionplans");

            migrationBuilder.DropColumn(
                name: "hasprioritysupport",
                table: "subscriptionplans");

            migrationBuilder.DropColumn(
                name: "maxservers",
                table: "subscriptionplans");

            migrationBuilder.DropColumn(
                name: "metricsretentiondays",
                table: "subscriptionplans");

            migrationBuilder.DropColumn(
                name: "monthlyprice",
                table: "subscriptionplans");

            migrationBuilder.DropColumn(
                name: "yearlyprice",
                table: "subscriptionplans");
        }
    }
}

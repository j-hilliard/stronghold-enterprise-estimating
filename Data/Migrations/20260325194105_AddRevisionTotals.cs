using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Stronghold.EnterpriseEstimating.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRevisionTotals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EquipCount",
                table: "EstimateRevisions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "EquipTotal",
                table: "EstimateRevisions",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "GrandTotal",
                table: "EstimateRevisions",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "LaborCount",
                table: "EstimateRevisions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "LaborTotal",
                table: "EstimateRevisions",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EquipCount",
                table: "EstimateRevisions");

            migrationBuilder.DropColumn(
                name: "EquipTotal",
                table: "EstimateRevisions");

            migrationBuilder.DropColumn(
                name: "GrandTotal",
                table: "EstimateRevisions");

            migrationBuilder.DropColumn(
                name: "LaborCount",
                table: "EstimateRevisions");

            migrationBuilder.DropColumn(
                name: "LaborTotal",
                table: "EstimateRevisions");
        }
    }
}

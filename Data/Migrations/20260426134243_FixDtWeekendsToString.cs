using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Stronghold.EnterpriseEstimating.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixDtWeekendsToString : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "DtWeekends",
                table: "StaffingPlans",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.Sql(
                "UPDATE [StaffingPlans] SET [DtWeekends] = CASE WHEN [DtWeekends] = '1' THEN 'sat_sun' ELSE 'none' END");

            migrationBuilder.AlterColumn<string>(
                name: "DtWeekends",
                table: "Estimates",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.Sql(
                "UPDATE [Estimates] SET [DtWeekends] = CASE WHEN [DtWeekends] = '1' THEN 'sat_sun' ELSE 'none' END");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<bool>(
                name: "DtWeekends",
                table: "StaffingPlans",
                type: "bit",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<bool>(
                name: "DtWeekends",
                table: "Estimates",
                type: "bit",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");
        }
    }
}

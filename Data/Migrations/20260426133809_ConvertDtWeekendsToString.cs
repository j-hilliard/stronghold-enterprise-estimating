using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Stronghold.EnterpriseEstimating.Data.Migrations
{
    /// <inheritdoc />
    public partial class ConvertDtWeekendsToString : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Estimates table
            migrationBuilder.AddColumn<string>(
                name: "DtWeekendsNew",
                table: "Estimates",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "none");

            migrationBuilder.Sql(
                "UPDATE [Estimates] SET [DtWeekendsNew] = CASE WHEN [DtWeekends] = 1 THEN N'sat_sun' ELSE N'none' END");

            migrationBuilder.DropColumn(name: "DtWeekends", table: "Estimates");

            migrationBuilder.RenameColumn(
                name: "DtWeekendsNew",
                table: "Estimates",
                newName: "DtWeekends");

            // StaffingPlans table
            migrationBuilder.AddColumn<string>(
                name: "DtWeekendsNew",
                table: "StaffingPlans",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "none");

            migrationBuilder.Sql(
                "UPDATE [StaffingPlans] SET [DtWeekendsNew] = CASE WHEN [DtWeekends] = 1 THEN N'sat_sun' ELSE N'none' END");

            migrationBuilder.DropColumn(name: "DtWeekends", table: "StaffingPlans");

            migrationBuilder.RenameColumn(
                name: "DtWeekendsNew",
                table: "StaffingPlans",
                newName: "DtWeekends");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Estimates table - revert string back to bit
            migrationBuilder.AddColumn<bool>(
                name: "DtWeekendsOld",
                table: "Estimates",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql(
                "UPDATE [Estimates] SET [DtWeekendsOld] = CASE WHEN [DtWeekends] = N'sat_sun' THEN 1 ELSE 0 END");

            migrationBuilder.DropColumn(name: "DtWeekends", table: "Estimates");

            migrationBuilder.RenameColumn(
                name: "DtWeekendsOld",
                table: "Estimates",
                newName: "DtWeekends");

            // StaffingPlans table - revert string back to bit
            migrationBuilder.AddColumn<bool>(
                name: "DtWeekendsOld",
                table: "StaffingPlans",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql(
                "UPDATE [StaffingPlans] SET [DtWeekendsOld] = CASE WHEN [DtWeekends] = N'sat_sun' THEN 1 ELSE 0 END");

            migrationBuilder.DropColumn(name: "DtWeekends", table: "StaffingPlans");

            migrationBuilder.RenameColumn(
                name: "DtWeekendsOld",
                table: "StaffingPlans",
                newName: "DtWeekends");
        }
    }
}

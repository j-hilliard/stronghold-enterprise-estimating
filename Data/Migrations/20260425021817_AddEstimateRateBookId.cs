using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Stronghold.EnterpriseEstimating.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEstimateRateBookId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RateBookId",
                table: "Estimates",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Estimates_RateBookId",
                table: "Estimates",
                column: "RateBookId");

            migrationBuilder.AddForeignKey(
                name: "FK_Estimates_RateBooks_RateBookId",
                table: "Estimates",
                column: "RateBookId",
                principalTable: "RateBooks",
                principalColumn: "RateBookId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Estimates_RateBooks_RateBookId",
                table: "Estimates");

            migrationBuilder.DropIndex(
                name: "IX_Estimates_RateBookId",
                table: "Estimates");

            migrationBuilder.DropColumn(
                name: "RateBookId",
                table: "Estimates");
        }
    }
}

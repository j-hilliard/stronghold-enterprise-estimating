using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Stronghold.EnterpriseEstimating.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRateBookExtensions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiresDate",
                table: "RateBooks",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "RateBookEquipmentRates",
                columns: table => new
                {
                    RateBookEquipmentRateId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RateBookId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Hourly = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    Daily = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    Weekly = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    Monthly = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RateBookEquipmentRates", x => x.RateBookEquipmentRateId);
                    table.ForeignKey(
                        name: "FK_RateBookEquipmentRates_RateBooks_RateBookId",
                        column: x => x.RateBookId,
                        principalTable: "RateBooks",
                        principalColumn: "RateBookId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RateBookExpenseItems",
                columns: table => new
                {
                    RateBookExpenseItemId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RateBookId = table.Column<int>(type: "int", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Rate = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RateBookExpenseItems", x => x.RateBookExpenseItemId);
                    table.ForeignKey(
                        name: "FK_RateBookExpenseItems_RateBooks_RateBookId",
                        column: x => x.RateBookId,
                        principalTable: "RateBooks",
                        principalColumn: "RateBookId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RateBookEquipmentRates_RateBookId",
                table: "RateBookEquipmentRates",
                column: "RateBookId");

            migrationBuilder.CreateIndex(
                name: "IX_RateBookExpenseItems_RateBookId",
                table: "RateBookExpenseItems",
                column: "RateBookId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RateBookEquipmentRates");

            migrationBuilder.DropTable(
                name: "RateBookExpenseItems");

            migrationBuilder.DropColumn(
                name: "ExpiresDate",
                table: "RateBooks");
        }
    }
}

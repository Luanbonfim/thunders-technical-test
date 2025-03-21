using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Thunders.TechTest.ApiService.Data;

namespace Thunders.TechTest.ApiService.Migrations;

[DbContext(typeof(TollUsageDbContext))]
[Migration("20240320000000_InitialCreate")]
public partial class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "TollUsages",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                UsageDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                TollBooth = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                State = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false),
                Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                VehicleType = table.Column<int>(type: "int", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_TollUsages", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_TollUsages_UsageDateTime_City",
            table: "TollUsages",
            columns: new[] { "UsageDateTime", "City" });

        migrationBuilder.CreateIndex(
            name: "IX_TollUsages_UsageDateTime_TollBooth",
            table: "TollUsages",
            columns: new[] { "UsageDateTime", "TollBooth" });

        migrationBuilder.CreateIndex(
            name: "IX_TollUsages_TollBooth_VehicleType",
            table: "TollUsages",
            columns: new[] { "TollBooth", "VehicleType" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "TollUsages");
    }
} 
using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Greenhouse.Infrastructure.Persistence.Migrations
{
    public partial class AddSunScheduleEntries : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SunScheduleEntries",
                columns: table => new
                {
                    Date = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    SunriseLocal = table.Column<string>(type: "TEXT", maxLength: 5, nullable: false),
                    SunsetLocal = table.Column<string>(type: "TEXT", maxLength: 5, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SunScheduleEntries", x => x.Date);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "SunScheduleEntries");
        }
    }
}

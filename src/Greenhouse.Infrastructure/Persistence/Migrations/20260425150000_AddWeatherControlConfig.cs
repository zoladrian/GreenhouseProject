using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Greenhouse.Infrastructure.Persistence.Migrations
{
    public partial class AddWeatherControlConfig : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WeatherControlConfigs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    RainDetectedMinRaw = table.Column<decimal>(type: "TEXT", nullable: false),
                    HighHumidityMinRaw = table.Column<decimal>(type: "TEXT", nullable: false),
                    SunnyMinRaw = table.Column<decimal>(type: "TEXT", nullable: false),
                    CloudyMaxRaw = table.Column<decimal>(type: "TEXT", nullable: false),
                    SunriseLocal = table.Column<string>(type: "TEXT", maxLength: 5, nullable: false),
                    SunsetLocal = table.Column<string>(type: "TEXT", maxLength: 5, nullable: false),
                    ManualRainStatus = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    ManualLightStatus = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeatherControlConfigs", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "WeatherControlConfigs",
                columns: new[]
                {
                    "Id", "RainDetectedMinRaw", "HighHumidityMinRaw", "SunnyMinRaw", "CloudyMaxRaw",
                    "SunriseLocal", "SunsetLocal", "ManualRainStatus", "ManualLightStatus", "UpdatedAtUtc"
                },
                values: new object[]
                {
                    1, 120m, 20m, 1600m, 1000m,
                    "06:00", "20:00", "auto", "auto", DateTime.UtcNow
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "WeatherControlConfigs");
        }
    }
}

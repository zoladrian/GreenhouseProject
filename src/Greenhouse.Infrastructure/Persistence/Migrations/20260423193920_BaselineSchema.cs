using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Greenhouse.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class BaselineSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Nawy",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    PlantNote = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    MoistureMin = table.Column<decimal>(type: "TEXT", nullable: true),
                    MoistureMax = table.Column<decimal>(type: "TEXT", nullable: true),
                    TemperatureMin = table.Column<decimal>(type: "TEXT", nullable: true),
                    TemperatureMax = table.Column<decimal>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Nawy", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Sensors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ExternalId = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 120, nullable: true),
                    Kind = table.Column<int>(type: "INTEGER", nullable: false),
                    NawaId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sensors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Sensors_Nawy_NawaId",
                        column: x => x.NawaId,
                        principalTable: "Nawy",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "SensorReadings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SensorIdentifier = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    ReceivedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Topic = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    RawPayloadJson = table.Column<string>(type: "TEXT", nullable: false),
                    SoilMoisture = table.Column<decimal>(type: "TEXT", nullable: true),
                    Temperature = table.Column<decimal>(type: "TEXT", nullable: true),
                    Battery = table.Column<int>(type: "INTEGER", nullable: true),
                    LinkQuality = table.Column<int>(type: "INTEGER", nullable: true),
                    Rain = table.Column<bool>(type: "INTEGER", nullable: true),
                    RainIntensityRaw = table.Column<decimal>(type: "TEXT", nullable: true),
                    IlluminanceRaw = table.Column<decimal>(type: "TEXT", nullable: true),
                    IlluminanceAverage20MinRaw = table.Column<decimal>(type: "TEXT", nullable: true),
                    IlluminanceMaximumTodayRaw = table.Column<decimal>(type: "TEXT", nullable: true),
                    CleaningReminder = table.Column<bool>(type: "INTEGER", nullable: true),
                    SensorId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SensorReadings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SensorReadings_Sensors_SensorId",
                        column: x => x.SensorId,
                        principalTable: "Sensors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Nawy_Name",
                table: "Nawy",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_SensorReadings_ReceivedAtUtc",
                table: "SensorReadings",
                column: "ReceivedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_SensorReadings_SensorId_ReceivedAtUtc",
                table: "SensorReadings",
                columns: new[] { "SensorId", "ReceivedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_SensorReadings_SensorIdentifier",
                table: "SensorReadings",
                column: "SensorIdentifier");

            migrationBuilder.CreateIndex(
                name: "IX_Sensors_ExternalId",
                table: "Sensors",
                column: "ExternalId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sensors_NawaId",
                table: "Sensors",
                column: "NawaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SensorReadings");

            migrationBuilder.DropTable(
                name: "Sensors");

            migrationBuilder.DropTable(
                name: "Nawy");
        }
    }
}

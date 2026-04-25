using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Greenhouse.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSensorReadingPayloadHash : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PayloadHash",
                table: "SensorReadings",
                type: "TEXT",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SensorReadings_SensorId_PayloadHash_ReceivedAtUtc",
                table: "SensorReadings",
                columns: new[] { "SensorId", "PayloadHash", "ReceivedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SensorReadings_SensorId_PayloadHash_ReceivedAtUtc",
                table: "SensorReadings");

            migrationBuilder.DropColumn(
                name: "PayloadHash",
                table: "SensorReadings");
        }
    }
}

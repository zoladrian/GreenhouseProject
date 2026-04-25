using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Greenhouse.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UnassignGlobalWeatherSensors : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Czujniki pogodowe są globalne — wcześniejsze przypisania do nawy były błędem modelu danych.
            migrationBuilder.Sql("UPDATE \"Sensors\" SET \"NawaId\" = NULL WHERE \"Kind\" = 2;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Nie przywracamy przypisań nawy (brak kopii zapasowej NawaId).
        }
    }
}

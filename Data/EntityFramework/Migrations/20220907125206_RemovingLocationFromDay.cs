using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuickTrack.Data.EntityFramework.Migrations
{
    public partial class RemovingLocationFromDay : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DayLocation");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DayLocation",
                columns: table => new
                {
                    DaysId = table.Column<int>(type: "INTEGER", nullable: false),
                    LocationsId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DayLocation", x => new { x.DaysId, x.LocationsId });
                    table.ForeignKey(
                        name: "FK_DayLocation_Days_DaysId",
                        column: x => x.DaysId,
                        principalTable: "Days",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DayLocation_Locations_LocationsId",
                        column: x => x.LocationsId,
                        principalTable: "Locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DayLocation_LocationsId",
                table: "DayLocation",
                column: "LocationsId");
        }
    }
}

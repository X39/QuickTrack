using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuickTrack.Data.EntityFramework.Migrations
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Days",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    Date_Day = table.Column<byte>(type: "INTEGER", nullable: false),
                    Date_Month = table.Column<byte>(type: "INTEGER", nullable: false),
                    Date_Year = table.Column<short>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Days", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Locations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    TimeStampCreated = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Locations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Projects",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    TimeStampCreated = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Projects", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Audits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DayFk = table.Column<int>(type: "INTEGER", nullable: false),
                    TimeStamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Message = table.Column<string>(type: "TEXT", nullable: false),
                    Kind = table.Column<int>(type: "INTEGER", nullable: false),
                    Json = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Audits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Audits_Days_DayFk",
                        column: x => x.DayFk,
                        principalTable: "Days",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "JsonAttachment<Day>",
                columns: table => new
                {
                    Realm = table.Column<string>(type: "TEXT", nullable: false),
                    ParentId = table.Column<int>(type: "INTEGER", nullable: true),
                    Json = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JsonAttachment<Day>", x => x.Realm);
                    table.ForeignKey(
                        name: "FK_JsonAttachment<Day>_Days_ParentId",
                        column: x => x.ParentId,
                        principalTable: "Days",
                        principalColumn: "Id");
                });

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

            migrationBuilder.CreateTable(
                name: "JsonAttachment<Location>",
                columns: table => new
                {
                    Realm = table.Column<string>(type: "TEXT", nullable: false),
                    ParentId = table.Column<int>(type: "INTEGER", nullable: true),
                    Json = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JsonAttachment<Location>", x => x.Realm);
                    table.ForeignKey(
                        name: "FK_JsonAttachment<Location>_Locations_ParentId",
                        column: x => x.ParentId,
                        principalTable: "Locations",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "JsonAttachment<Project>",
                columns: table => new
                {
                    Realm = table.Column<string>(type: "TEXT", nullable: false),
                    ParentId = table.Column<int>(type: "INTEGER", nullable: true),
                    Json = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JsonAttachment<Project>", x => x.Realm);
                    table.ForeignKey(
                        name: "FK_JsonAttachment<Project>_Projects_ParentId",
                        column: x => x.ParentId,
                        principalTable: "Projects",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TimeLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DayFk = table.Column<int>(type: "INTEGER", nullable: false),
                    ProjectFk = table.Column<int>(type: "INTEGER", nullable: false),
                    LocationFk = table.Column<int>(type: "INTEGER", nullable: false),
                    TimeStamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Message = table.Column<string>(type: "TEXT", nullable: false),
                    Mode = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TimeLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TimeLogs_Days_DayFk",
                        column: x => x.DayFk,
                        principalTable: "Days",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TimeLogs_Locations_LocationFk",
                        column: x => x.LocationFk,
                        principalTable: "Locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TimeLogs_Projects_ProjectFk",
                        column: x => x.ProjectFk,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "JsonAttachment<TimeLog>",
                columns: table => new
                {
                    Realm = table.Column<string>(type: "TEXT", nullable: false),
                    ParentId = table.Column<int>(type: "INTEGER", nullable: true),
                    Json = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JsonAttachment<TimeLog>", x => x.Realm);
                    table.ForeignKey(
                        name: "FK_JsonAttachment<TimeLog>_TimeLogs_ParentId",
                        column: x => x.ParentId,
                        principalTable: "TimeLogs",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Audits_DayFk",
                table: "Audits",
                column: "DayFk");

            migrationBuilder.CreateIndex(
                name: "IX_DayLocation_LocationsId",
                table: "DayLocation",
                column: "LocationsId");

            migrationBuilder.CreateIndex(
                name: "IX_Days_Date_Day_Date_Month_Date_Year",
                table: "Days",
                columns: new[] { "Date_Day", "Date_Month", "Date_Year" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Days_Date_Month_Date_Year",
                table: "Days",
                columns: new[] { "Date_Month", "Date_Year" });

            migrationBuilder.CreateIndex(
                name: "IX_Days_Date_Year",
                table: "Days",
                column: "Date_Year");

            migrationBuilder.CreateIndex(
                name: "IX_JsonAttachment<Day>_ParentId",
                table: "JsonAttachment<Day>",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_JsonAttachment<Location>_ParentId",
                table: "JsonAttachment<Location>",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_JsonAttachment<Project>_ParentId",
                table: "JsonAttachment<Project>",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_JsonAttachment<TimeLog>_ParentId",
                table: "JsonAttachment<TimeLog>",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_Locations_Title",
                table: "Locations",
                column: "Title",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Projects_Title",
                table: "Projects",
                column: "Title",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TimeLogs_DayFk",
                table: "TimeLogs",
                column: "DayFk");

            migrationBuilder.CreateIndex(
                name: "IX_TimeLogs_LocationFk",
                table: "TimeLogs",
                column: "LocationFk");

            migrationBuilder.CreateIndex(
                name: "IX_TimeLogs_Message",
                table: "TimeLogs",
                column: "Message");

            migrationBuilder.CreateIndex(
                name: "IX_TimeLogs_ProjectFk",
                table: "TimeLogs",
                column: "ProjectFk");

            migrationBuilder.CreateIndex(
                name: "IX_TimeLogs_TimeStamp",
                table: "TimeLogs",
                column: "TimeStamp");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Audits");

            migrationBuilder.DropTable(
                name: "DayLocation");

            migrationBuilder.DropTable(
                name: "JsonAttachment<Day>");

            migrationBuilder.DropTable(
                name: "JsonAttachment<Location>");

            migrationBuilder.DropTable(
                name: "JsonAttachment<Project>");

            migrationBuilder.DropTable(
                name: "JsonAttachment<TimeLog>");

            migrationBuilder.DropTable(
                name: "TimeLogs");

            migrationBuilder.DropTable(
                name: "Days");

            migrationBuilder.DropTable(
                name: "Locations");

            migrationBuilder.DropTable(
                name: "Projects");
        }
    }
}

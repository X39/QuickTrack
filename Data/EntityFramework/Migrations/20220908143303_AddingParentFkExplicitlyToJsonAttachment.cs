using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuickTrack.Data.EntityFramework.Migrations
{
    public partial class AddingParentFkExplicitlyToJsonAttachment : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_JsonAttachment<Day>_Days_ParentId",
                table: "JsonAttachment<Day>");

            migrationBuilder.DropForeignKey(
                name: "FK_JsonAttachment<Location>_Locations_ParentId",
                table: "JsonAttachment<Location>");

            migrationBuilder.DropForeignKey(
                name: "FK_JsonAttachment<Project>_Projects_ParentId",
                table: "JsonAttachment<Project>");

            migrationBuilder.DropForeignKey(
                name: "FK_JsonAttachment<TimeLog>_TimeLogs_ParentId",
                table: "JsonAttachment<TimeLog>");

            migrationBuilder.DropPrimaryKey(
                name: "PK_JsonAttachment<TimeLog>",
                table: "JsonAttachment<TimeLog>");

            migrationBuilder.DropIndex(
                name: "IX_JsonAttachment<TimeLog>_ParentId",
                table: "JsonAttachment<TimeLog>");

            migrationBuilder.DropPrimaryKey(
                name: "PK_JsonAttachment<Project>",
                table: "JsonAttachment<Project>");

            migrationBuilder.DropIndex(
                name: "IX_JsonAttachment<Project>_ParentId",
                table: "JsonAttachment<Project>");

            migrationBuilder.DropPrimaryKey(
                name: "PK_JsonAttachment<Location>",
                table: "JsonAttachment<Location>");

            migrationBuilder.DropIndex(
                name: "IX_JsonAttachment<Location>_ParentId",
                table: "JsonAttachment<Location>");

            migrationBuilder.DropPrimaryKey(
                name: "PK_JsonAttachment<Day>",
                table: "JsonAttachment<Day>");

            migrationBuilder.DropIndex(
                name: "IX_JsonAttachment<Day>_ParentId",
                table: "JsonAttachment<Day>");

            migrationBuilder.DropColumn(
                name: "ParentId",
                table: "JsonAttachment<TimeLog>");

            migrationBuilder.DropColumn(
                name: "ParentId",
                table: "JsonAttachment<Project>");

            migrationBuilder.DropColumn(
                name: "ParentId",
                table: "JsonAttachment<Location>");

            migrationBuilder.DropColumn(
                name: "ParentId",
                table: "JsonAttachment<Day>");

            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "JsonAttachment<TimeLog>",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0)
                .Annotation("Sqlite:Autoincrement", true);

            migrationBuilder.AddColumn<int>(
                name: "ParentFk",
                table: "JsonAttachment<TimeLog>",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "JsonAttachment<Project>",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0)
                .Annotation("Sqlite:Autoincrement", true);

            migrationBuilder.AddColumn<int>(
                name: "ParentFk",
                table: "JsonAttachment<Project>",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "JsonAttachment<Location>",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0)
                .Annotation("Sqlite:Autoincrement", true);

            migrationBuilder.AddColumn<int>(
                name: "ParentFk",
                table: "JsonAttachment<Location>",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "JsonAttachment<Day>",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0)
                .Annotation("Sqlite:Autoincrement", true);

            migrationBuilder.AddColumn<int>(
                name: "ParentFk",
                table: "JsonAttachment<Day>",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_JsonAttachment<TimeLog>",
                table: "JsonAttachment<TimeLog>",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_JsonAttachment<Project>",
                table: "JsonAttachment<Project>",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_JsonAttachment<Location>",
                table: "JsonAttachment<Location>",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_JsonAttachment<Day>",
                table: "JsonAttachment<Day>",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_JsonAttachment<TimeLog>_ParentFk",
                table: "JsonAttachment<TimeLog>",
                column: "ParentFk");

            migrationBuilder.CreateIndex(
                name: "IX_JsonAttachment<TimeLog>_Realm_ParentFk",
                table: "JsonAttachment<TimeLog>",
                columns: new[] { "Realm", "ParentFk" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_JsonAttachment<Project>_ParentFk",
                table: "JsonAttachment<Project>",
                column: "ParentFk");

            migrationBuilder.CreateIndex(
                name: "IX_JsonAttachment<Project>_Realm_ParentFk",
                table: "JsonAttachment<Project>",
                columns: new[] { "Realm", "ParentFk" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_JsonAttachment<Location>_ParentFk",
                table: "JsonAttachment<Location>",
                column: "ParentFk");

            migrationBuilder.CreateIndex(
                name: "IX_JsonAttachment<Location>_Realm_ParentFk",
                table: "JsonAttachment<Location>",
                columns: new[] { "Realm", "ParentFk" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_JsonAttachment<Day>_ParentFk",
                table: "JsonAttachment<Day>",
                column: "ParentFk");

            migrationBuilder.CreateIndex(
                name: "IX_JsonAttachment<Day>_Realm_ParentFk",
                table: "JsonAttachment<Day>",
                columns: new[] { "Realm", "ParentFk" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_JsonAttachment<Day>_Days_ParentFk",
                table: "JsonAttachment<Day>",
                column: "ParentFk",
                principalTable: "Days",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_JsonAttachment<Location>_Locations_ParentFk",
                table: "JsonAttachment<Location>",
                column: "ParentFk",
                principalTable: "Locations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_JsonAttachment<Project>_Projects_ParentFk",
                table: "JsonAttachment<Project>",
                column: "ParentFk",
                principalTable: "Projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_JsonAttachment<TimeLog>_TimeLogs_ParentFk",
                table: "JsonAttachment<TimeLog>",
                column: "ParentFk",
                principalTable: "TimeLogs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_JsonAttachment<Day>_Days_ParentFk",
                table: "JsonAttachment<Day>");

            migrationBuilder.DropForeignKey(
                name: "FK_JsonAttachment<Location>_Locations_ParentFk",
                table: "JsonAttachment<Location>");

            migrationBuilder.DropForeignKey(
                name: "FK_JsonAttachment<Project>_Projects_ParentFk",
                table: "JsonAttachment<Project>");

            migrationBuilder.DropForeignKey(
                name: "FK_JsonAttachment<TimeLog>_TimeLogs_ParentFk",
                table: "JsonAttachment<TimeLog>");

            migrationBuilder.DropPrimaryKey(
                name: "PK_JsonAttachment<TimeLog>",
                table: "JsonAttachment<TimeLog>");

            migrationBuilder.DropIndex(
                name: "IX_JsonAttachment<TimeLog>_ParentFk",
                table: "JsonAttachment<TimeLog>");

            migrationBuilder.DropIndex(
                name: "IX_JsonAttachment<TimeLog>_Realm_ParentFk",
                table: "JsonAttachment<TimeLog>");

            migrationBuilder.DropPrimaryKey(
                name: "PK_JsonAttachment<Project>",
                table: "JsonAttachment<Project>");

            migrationBuilder.DropIndex(
                name: "IX_JsonAttachment<Project>_ParentFk",
                table: "JsonAttachment<Project>");

            migrationBuilder.DropIndex(
                name: "IX_JsonAttachment<Project>_Realm_ParentFk",
                table: "JsonAttachment<Project>");

            migrationBuilder.DropPrimaryKey(
                name: "PK_JsonAttachment<Location>",
                table: "JsonAttachment<Location>");

            migrationBuilder.DropIndex(
                name: "IX_JsonAttachment<Location>_ParentFk",
                table: "JsonAttachment<Location>");

            migrationBuilder.DropIndex(
                name: "IX_JsonAttachment<Location>_Realm_ParentFk",
                table: "JsonAttachment<Location>");

            migrationBuilder.DropPrimaryKey(
                name: "PK_JsonAttachment<Day>",
                table: "JsonAttachment<Day>");

            migrationBuilder.DropIndex(
                name: "IX_JsonAttachment<Day>_ParentFk",
                table: "JsonAttachment<Day>");

            migrationBuilder.DropIndex(
                name: "IX_JsonAttachment<Day>_Realm_ParentFk",
                table: "JsonAttachment<Day>");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "JsonAttachment<TimeLog>");

            migrationBuilder.DropColumn(
                name: "ParentFk",
                table: "JsonAttachment<TimeLog>");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "JsonAttachment<Project>");

            migrationBuilder.DropColumn(
                name: "ParentFk",
                table: "JsonAttachment<Project>");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "JsonAttachment<Location>");

            migrationBuilder.DropColumn(
                name: "ParentFk",
                table: "JsonAttachment<Location>");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "JsonAttachment<Day>");

            migrationBuilder.DropColumn(
                name: "ParentFk",
                table: "JsonAttachment<Day>");

            migrationBuilder.AddColumn<int>(
                name: "ParentId",
                table: "JsonAttachment<TimeLog>",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ParentId",
                table: "JsonAttachment<Project>",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ParentId",
                table: "JsonAttachment<Location>",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ParentId",
                table: "JsonAttachment<Day>",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_JsonAttachment<TimeLog>",
                table: "JsonAttachment<TimeLog>",
                column: "Realm");

            migrationBuilder.AddPrimaryKey(
                name: "PK_JsonAttachment<Project>",
                table: "JsonAttachment<Project>",
                column: "Realm");

            migrationBuilder.AddPrimaryKey(
                name: "PK_JsonAttachment<Location>",
                table: "JsonAttachment<Location>",
                column: "Realm");

            migrationBuilder.AddPrimaryKey(
                name: "PK_JsonAttachment<Day>",
                table: "JsonAttachment<Day>",
                column: "Realm");

            migrationBuilder.CreateIndex(
                name: "IX_JsonAttachment<TimeLog>_ParentId",
                table: "JsonAttachment<TimeLog>",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_JsonAttachment<Project>_ParentId",
                table: "JsonAttachment<Project>",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_JsonAttachment<Location>_ParentId",
                table: "JsonAttachment<Location>",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_JsonAttachment<Day>_ParentId",
                table: "JsonAttachment<Day>",
                column: "ParentId");

            migrationBuilder.AddForeignKey(
                name: "FK_JsonAttachment<Day>_Days_ParentId",
                table: "JsonAttachment<Day>",
                column: "ParentId",
                principalTable: "Days",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_JsonAttachment<Location>_Locations_ParentId",
                table: "JsonAttachment<Location>",
                column: "ParentId",
                principalTable: "Locations",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_JsonAttachment<Project>_Projects_ParentId",
                table: "JsonAttachment<Project>",
                column: "ParentId",
                principalTable: "Projects",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_JsonAttachment<TimeLog>_TimeLogs_ParentId",
                table: "JsonAttachment<TimeLog>",
                column: "ParentId",
                principalTable: "TimeLogs",
                principalColumn: "Id");
        }
    }
}

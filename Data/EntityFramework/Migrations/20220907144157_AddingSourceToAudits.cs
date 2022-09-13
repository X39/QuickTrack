using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuickTrack.Data.EntityFramework.Migrations
{
    public partial class AddingSourceToAudits : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Source",
                table: "Audits",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Source",
                table: "Audits");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Crowd_knowledge_contribution.Data.Migrations
{
    public partial class Restrictionare : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Restriction",
                table: "Articles",
                type: "bit",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Restriction",
                table: "Articles");
        }
    }
}

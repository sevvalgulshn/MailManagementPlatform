using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Project2EmailNight.Migrations
{
   
    public partial class AddCategoryColorHex : Migration
    {
       
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ColorHex",
                table: "Categories",
                type: "nvarchar(max)",
                nullable: true);
        }

    
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ColorHex",
                table: "Categories");
        }
    }
}

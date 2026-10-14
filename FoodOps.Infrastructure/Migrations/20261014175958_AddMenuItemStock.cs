using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodOps.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMenuItemStock : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Stock",
                table: "MenuItems",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Stock",
                table: "MenuItems");
        }
    }
}

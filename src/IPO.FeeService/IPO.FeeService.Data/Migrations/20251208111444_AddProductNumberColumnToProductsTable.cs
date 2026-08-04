using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IPO.FeeService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddProductNumberColumnToProductsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ProductNumber",
                table: "Products",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProductNumber",
                table: "Products");
        }
    }
}

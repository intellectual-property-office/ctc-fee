using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IPO.FeeService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRuleColumnToFeesTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Rule",
                table: "Fees",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "No Rule Effective Fee = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Rule",
                table: "Fees");
        }
    }
}

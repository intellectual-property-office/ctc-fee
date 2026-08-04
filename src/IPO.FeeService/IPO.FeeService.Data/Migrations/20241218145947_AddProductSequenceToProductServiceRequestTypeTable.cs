using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IPO.FeeService.Data.Migrations
{
    public partial class AddProductSequenceToProductServiceRequestTypeTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "ServiceRequestTypesId",
                table: "ProductServiceRequestType",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("Relational:ColumnOrder", 1);

            migrationBuilder.AlterColumn<int>(
                name: "ProductsId",
                table: "ProductServiceRequestType",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("Relational:ColumnOrder", 0);

            migrationBuilder.AddColumn<int>(
                name: "ProductSequence",
                table: "ProductServiceRequestType",
                type: "int",
                nullable: false,
                defaultValue: 1);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProductSequence",
                table: "ProductServiceRequestType");

            migrationBuilder.AlterColumn<int>(
                name: "ServiceRequestTypesId",
                table: "ProductServiceRequestType",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .OldAnnotation("Relational:ColumnOrder", 1);

            migrationBuilder.AlterColumn<int>(
                name: "ProductsId",
                table: "ProductServiceRequestType",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .OldAnnotation("Relational:ColumnOrder", 0);
        }
    }
}

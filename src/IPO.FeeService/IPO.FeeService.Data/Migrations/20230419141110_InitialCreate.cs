using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IPO.FeeService.Data.Migrations
{
    public partial class InitialCreate : Migration
    {
        [ExcludeFromCodeCoverage]
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Channel = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "QuantityRules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Rule = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuantityRules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ServiceRequestTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(35)", maxLength: 35, nullable: false),
                    E5AccountNumber = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceRequestTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TaxCodes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    Rate = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxCodes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Fees",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PriceExVat = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ProductId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Fees", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Fees_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductQuantityRule",
                columns: table => new
                {
                    ProductsId = table.Column<int>(type: "int", nullable: false),
                    QuantityRulesId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductQuantityRule", x => new { x.ProductsId, x.QuantityRulesId });
                    table.ForeignKey(
                        name: "FK_ProductQuantityRule_Products_ProductsId",
                        column: x => x.ProductsId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductQuantityRule_QuantityRules_QuantityRulesId",
                        column: x => x.QuantityRulesId,
                        principalTable: "QuantityRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductServiceRequestType",
                columns: table => new
                {
                    ProductsId = table.Column<int>(type: "int", nullable: false),
                    ServiceRequestTypesId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductServiceRequestType", x => new { x.ProductsId, x.ServiceRequestTypesId });
                    table.ForeignKey(
                        name: "FK_ProductServiceRequestType_Products_ProductsId",
                        column: x => x.ProductsId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductServiceRequestType_ServiceRequestTypes_ServiceRequestTypesId",
                        column: x => x.ServiceRequestTypesId,
                        principalTable: "ServiceRequestTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductTaxCode",
                columns: table => new
                {
                    ProductsId = table.Column<int>(type: "int", nullable: false),
                    TaxCodesId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductTaxCode", x => new { x.ProductsId, x.TaxCodesId });
                    table.ForeignKey(
                        name: "FK_ProductTaxCode_Products_ProductsId",
                        column: x => x.ProductsId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductTaxCode_TaxCodes_TaxCodesId",
                        column: x => x.TaxCodesId,
                        principalTable: "TaxCodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Fees_ProductId",
                table: "Fees",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductQuantityRule_QuantityRulesId",
                table: "ProductQuantityRule",
                column: "QuantityRulesId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductServiceRequestType_ServiceRequestTypesId",
                table: "ProductServiceRequestType",
                column: "ServiceRequestTypesId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductTaxCode_TaxCodesId",
                table: "ProductTaxCode",
                column: "TaxCodesId");
        }

        [ExcludeFromCodeCoverage]
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Fees");

            migrationBuilder.DropTable(
                name: "ProductQuantityRule");

            migrationBuilder.DropTable(
                name: "ProductServiceRequestType");

            migrationBuilder.DropTable(
                name: "ProductTaxCode");

            migrationBuilder.DropTable(
                name: "QuantityRules");

            migrationBuilder.DropTable(
                name: "ServiceRequestTypes");

            migrationBuilder.DropTable(
                name: "Products");

            migrationBuilder.DropTable(
                name: "TaxCodes");
        }
    }
}

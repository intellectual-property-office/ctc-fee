using Swashbuckle.AspNetCore.Annotations;
using System.Text.Json.Serialization;

namespace IPO.FeeService.Models.API
{
    [SwaggerSchema(Description = "This is an individual line item breakdown for the Calculate endpoint")]
    public class FeeItem
    {
        [SwaggerSchema(Title = "The line item number")]
        public int LineNum { get; set; }

        [SwaggerSchema(Title = "The name of the product")]
        public string? ProductName { get; set; }

        [SwaggerSchema(Title = "The E5 product code")]
        public string? E5ProductCode { get; set; }

        [SwaggerSchema(Title = "The quantity of products")]
        public int Quantity { get; set; }

        [SwaggerSchema(Title = "The price per product")]
        public decimal Price { get; set; }

        [SwaggerSchema(Title = "The total price for the products (including VAT)")]
        public decimal LineTotal { get; set; }

        [SwaggerSchema(Title = "A code representing the VAT rate name for the product. Example \"OS\", Out of Scope, \"STS\", VAT Output Sales, etc.")]
        public string? VatCode { get; set; }

        [SwaggerSchema(Title = "The VAT amount chargeable")]
        public decimal VatAmount { get; set; }

        [JsonIgnore]
        public int ProductSequence { get; set; }

		[SwaggerSchema(Title = "The product details")]
        public object? ProductDetails { get; set; }
    }
}

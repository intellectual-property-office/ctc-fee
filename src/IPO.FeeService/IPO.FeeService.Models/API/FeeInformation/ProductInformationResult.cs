using Swashbuckle.AspNetCore.Annotations;
using System.Collections.Generic;

namespace IPO.FeeService.Models.API.FeeInformation
{
    [SwaggerSchema(Description = "Represents fee information and associated details for a single product")]
    public class ProductInformationResult
    {
        public ProductInformationResult(
            int number,
            string channel,
            string name,
            string e5ProductCode,
            decimal recentExVat,
            decimal recentVat,
            List<ProductInformationResultDetails> details)
        {
            Number = number;
            Channel = channel;
            Name = name;
            E5ProductCode = e5ProductCode;
            RecentExVat = recentExVat;
            RecentVat = recentVat;
            Details = details;
        }

        [SwaggerSchema( Title = "The unique product number used to identify the product" )]
        public int Number { get; set; }

        [SwaggerSchema(Title = "The channel through which this product can be supplied")]
        public string Channel { get; set; }

        [SwaggerSchema(Title = "Product name")]
        public string Name { get; set; }

        [SwaggerSchema(Title = "E5 product code")]
        public string E5ProductCode { get; set; }

        [SwaggerSchema(Title = "The most recent fee amount currently in effect for this product (excluding VAT)")]
        public decimal RecentExVat { get; set; }

        [SwaggerSchema(Title = "The VAT amount calculated from the most recent fee currently in effect for this product")]
        public decimal RecentVat { get; set; }

        [SwaggerSchema(Title = "The total of the most recent fee currently in effect for this product (including VAT)")]
        public decimal RecentIncVat => RecentExVat + RecentVat;

        [SwaggerSchema(Title = "A list of fee, tax, quantity rule and service request information for this product")]
        public List<ProductInformationResultDetails> Details { get; set; }
    }

}

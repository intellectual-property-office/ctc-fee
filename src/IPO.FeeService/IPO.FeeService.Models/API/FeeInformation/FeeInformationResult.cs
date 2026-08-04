using Swashbuckle.AspNetCore.Annotations;
using System.Collections.Generic;

namespace IPO.FeeService.Models.API.FeeInformation
{
    [SwaggerSchema(Description = "Represents the full response returned by the byProductNumber endpoint")]
    public class FeeInformationResult
    {
        public FeeInformationResult(
            string effectiveDate,
            string customerChannel,
            decimal totalRecentExVat,
            decimal totalRecentVat,
            decimal totalRecentIncVat,
            List<ProductInformationResult> products)
        {
            EffectiveDate = effectiveDate;
            CustomerChannel = customerChannel;
            TotalRecentExVat = totalRecentExVat;
            TotalRecentVat = totalRecentVat;
            TotalRecentIncVat = totalRecentIncVat;
            Products = products;
        }

        [SwaggerSchema(Title = "The effective date supplied in the request", Format = "\"yyyy-MM-dd\"")]
        public string EffectiveDate { get; set; } = string.Empty;

        [SwaggerSchema(Title = "The customer channel supplied in the request")]
        public string CustomerChannel { get; set; } = string.Empty;

        [SwaggerSchema(
            Title = "Total of all most‑recent, in‑effect fees (excluding VAT)",
            Description = "Calculated by summing the price (excluding VAT) of each product’s most recent fee that is currently in effect"
        )]
        public decimal TotalRecentExVat { get; set; }

        [SwaggerSchema(
            Title = "Total VAT for all most‑recent, in‑effect fees",
            Description = "Calculated by summing the VAT amount associated with each product’s most recent fee that is currently in effect"
        )]
        public decimal TotalRecentVat { get; set; }

        [SwaggerSchema(
            Title = "Total of all most‑recent, in‑effect fees (including VAT)",
            Description = "Calculated by summing the price (including VAT) of each product’s most recent fee that is currently in effect"
        )]
        public decimal TotalRecentIncVat { get; set; }

        [SwaggerSchema(
            Title = "The list of products included in the response",
            Description = "Each item contains detailed fee, tax, and quantity rule information for a single product"
        )]
        public List<ProductInformationResult> Products { get; set; } = new();
    }

}

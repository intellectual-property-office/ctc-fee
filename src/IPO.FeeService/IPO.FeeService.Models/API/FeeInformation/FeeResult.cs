using IPO.FeeService.Models.Data;
using Swashbuckle.AspNetCore.Annotations;
using System;

namespace IPO.FeeService.Models.API.FeeInformation
{
    [SwaggerSchema(Description = "Represents the fee information for a product")]
    public class FeeResult
    {
        public FeeResult(Fee? fee, decimal? taxRate)
        {
            if (fee != null)
            {
                ExVat = fee?.PriceExVat ?? 0m;
                var rate = taxRate ?? 0m;
                Vat = decimal.Round((decimal)(rate * ExVat * 0.01m), 2);
                Rule = fee?.Rule;
                EffectiveFrom = fee?.EffectiveFrom ?? DateTime.MinValue;
                EffectiveTo = fee?.EffectiveTo;
            }
        }
        [SwaggerSchema(Title = "Fee amount (excluding VAT)")] 
        public decimal? ExVat { get; set; } = null; 

        [SwaggerSchema(Title = "VAT amount")] 
        public decimal Vat { get; set; } = 0m; 

        [SwaggerSchema(Title = "Fee amount (including VAT)")] 
        public decimal IncVat => ExVat.HasValue ? ExVat.Value + Vat : 0m; 

        [SwaggerSchema(Title = "Fee rule")] 
        public string? Rule { get; set; } = null; 

        [SwaggerSchema(Title = "Effective from date", Format = "\"yyyy-MM-dd\"")] 
        public DateTime? EffectiveFrom { get; set; } = null; 

        [SwaggerSchema(Title = "Effective to date", Format = "\"yyyy-MM-dd\"")] 
        public DateTime? EffectiveTo { get; set; } = null;
    }
}

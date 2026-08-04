using IPO.FeeService.Models.Data;
using Swashbuckle.AspNetCore.Annotations;
using System;

namespace IPO.FeeService.Models.API.FeeInformation
{
    [SwaggerSchema(Description = "Represents the quantity rule and its effective dates for a product")]
    public class QuantityRulesResult
    {
        public QuantityRulesResult(QuantityRule quantityRule)
        {
            if (quantityRule != null)
            {
                Rule = quantityRule?.Rule;
                EffectiveFrom = quantityRule?.EffectiveFrom;
                EffectiveTo = quantityRule?.EffectiveTo;
            }
        }

        [SwaggerSchema(Title = "Quantity rule")]
        public string? Rule { get; set; } = null;

        [SwaggerSchema(Title = "Effective from date", Format = "\"yyyy-MM-dd\"")]
        public DateTime? EffectiveFrom { get; set; } = null;

        [SwaggerSchema(Title = "Effective to date", Format = "\"yyyy-MM-dd\"")]
        public DateTime? EffectiveTo { get; set; } = null;
    }

}

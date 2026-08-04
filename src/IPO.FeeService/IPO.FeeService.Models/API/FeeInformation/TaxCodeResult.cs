using IPO.FeeService.Models.Data;
using Swashbuckle.AspNetCore.Annotations;
using System;

namespace IPO.FeeService.Models.API.FeeInformation
{
    [SwaggerSchema(Description = "Represents the tax code applied to a product, including rate and effective dates")]
    public class TaxCodeResult
    {
        public TaxCodeResult(TaxCode taxCode)
        {
            if (taxCode != null)
            {
                Code = taxCode.Code;
                Description = taxCode.Description;
                Rate = taxCode.Rate;
                EffectiveFrom = taxCode.EffectiveFrom;
                EffectiveTo = taxCode.EffectiveTo;
            }
        }

        [SwaggerSchema(Title = "Tax code")]
        public string? Code { get; set; } = null;

        [SwaggerSchema(Title = "Tax code description")]
        public string? Description { get; set; } = null;

        [SwaggerSchema(Title = "Tax rate")]
        public decimal? Rate { get; set; } = null;

        [SwaggerSchema(Title = "Effective from date", Format = "\"yyyy-MM-dd\"")]
        public DateTime? EffectiveFrom { get; set; } = null;

        [SwaggerSchema(Title = "Effective to date", Format = "\"yyyy-MM-dd\"")]
        public DateTime? EffectiveTo { get; set; } = null;
    }

}

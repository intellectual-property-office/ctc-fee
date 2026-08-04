using Swashbuckle.AspNetCore.Annotations;
using System;
using System.ComponentModel.DataAnnotations;

namespace IPO.FeeService.Models.API.FeeInformation
{
    [SwaggerSchema(Description = "Represents the request used to retrieve fee information for one or more products")]
    public class FeeInformationRequest
    {
        [SwaggerSchema(Title = "Date for which active fee, tax, and quantity rule records should be returned (defaults to today's date)", Format = "\"yyyy-MM-dd\"", Nullable = true)]
        public string EffectiveDate { get; set; } = DateTime.Now.ToString("yyyy-MM-dd");

        [Required]
        [SwaggerSchema(Title = "Product numbers for which fee, tax, and quantity rule records should be returned")]
        public int[]? ProductNumbers { get; set; }
    }
}

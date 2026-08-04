using System;
using Swashbuckle.AspNetCore.Annotations;

namespace IPO.FeeService.Models.API
{
    [SwaggerSchema("Calculate Fee Request Object, all values are optional dependent on ServiceRequestType", Required = null)]
    public class FeeCalculationRequest
    {

        [SwaggerSchema(Title = "The date on which payment was made. Default = today()", Format = "\"yyyy-MM-dd\"", Nullable = true)]
        public string PaidDate { get; set; } = DateTime.Now.ToString("yyyy-MM-dd");

        [SwaggerSchema(Title = "The details of each calculation request")]
        public FeeCalculationRequestDetails[]? RequestDetails { get; set; }
    }
}

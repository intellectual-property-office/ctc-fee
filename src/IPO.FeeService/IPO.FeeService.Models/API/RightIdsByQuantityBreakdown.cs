using Swashbuckle.AspNetCore.Annotations;

namespace IPO.FeeService.Models.API
{
    [SwaggerSchema(Description = "This is a renewals quantity item for a collection of Rights for the Calculate endpoint")]
    public class RightIdsByQuantityBreakdown
    {
        public RenewalRightsQuantityItem[]? RightIdsByQuantity { get; set; }
    }
}

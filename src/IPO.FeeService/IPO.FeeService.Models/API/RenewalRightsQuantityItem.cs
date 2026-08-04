using Swashbuckle.AspNetCore.Annotations;
using System.Collections.Generic;

namespace IPO.FeeService.Models.API
{
    [SwaggerSchema(Description = "This is a renewals quantity item for a collection of Rights for the Calculate endpoint")]
    public class RenewalRightsQuantityItem
    {
        [SwaggerSchema(Title = "The quantity for each right")]
        public int Quantity { get; set; }

        [SwaggerSchema(Title = "The list of right IDs which the quantity applies to")]
        public List<int>? RightIds { get; set; }
    }
}

using IPO.FeeService.Models.Data;
using Swashbuckle.AspNetCore.Annotations;

namespace IPO.FeeService.Models.API.FeeInformation
{
    [SwaggerSchema(Description = "Represents the product to service request type relationship")]
    public class ProductServiceRequestTypeResult
    {
        public ProductServiceRequestTypeResult(ProductServiceRequestType productServiceRequestType)
        {
            if (productServiceRequestType != null)
            {
                ProductSequence = productServiceRequestType?.ProductSequence;
            }
        }
        [SwaggerSchema(Title = "Sequence number that defines the product’s ordering position within a service request type")]
        public int? ProductSequence { get; set; } = null;
    }
}

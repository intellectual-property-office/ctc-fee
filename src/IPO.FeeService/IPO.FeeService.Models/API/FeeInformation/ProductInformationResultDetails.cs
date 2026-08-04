using Swashbuckle.AspNetCore.Annotations;

namespace IPO.FeeService.Models.API.FeeInformation
{
    [SwaggerSchema(Description = "Provides fee, tax, service request, and quantity rule information for a product within the response")]
    public class ProductInformationResultDetails
    {
        public ProductInformationResultDetails(
            ServiceRequestTypeResult serviceRequestType,
            ProductServiceRequestTypeResult productServiceRequestType,
            FeeResult fee,
            TaxCodeResult taxCode,
            QuantityRulesResult quantityRules)
        {
            ServiceRequestType = serviceRequestType;
            ProductServiceRequestType = productServiceRequestType;
            Fee = fee;
            TaxCode = taxCode;
            QuantityRules = quantityRules;
        }

        [SwaggerSchema(Title = "Service request type details")] 
        public ServiceRequestTypeResult ServiceRequestType { get; set; }

        [SwaggerSchema(Title = "Product to service request type relationship details")] 
        public ProductServiceRequestTypeResult ProductServiceRequestType { get; set; }

        [SwaggerSchema(Title = "Fee details")] 
        public FeeResult Fee { get; set; }

        [SwaggerSchema(Title = "Tax code details")] 
        public TaxCodeResult TaxCode { get; set; }

        [SwaggerSchema(Title = "Quantity rules")] 
        public QuantityRulesResult QuantityRules { get; set; }
    }
}

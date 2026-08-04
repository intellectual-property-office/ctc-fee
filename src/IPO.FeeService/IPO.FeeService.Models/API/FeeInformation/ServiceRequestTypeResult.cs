using Swashbuckle.AspNetCore.Annotations;

namespace IPO.FeeService.Models.API.FeeInformation
{
    [SwaggerSchema(Description = "Represents a service request type and its associated account information")]
    public class ServiceRequestTypeResult
    {
        public ServiceRequestTypeResult(Data.ServiceRequestType serviceRequestType)
        {
            if (serviceRequestType != null)
            {
                Name = serviceRequestType.Name;
                E5AccountNumber = serviceRequestType.E5AccountNumber;
            }
        }

        [SwaggerSchema(Title = "Service request type name")]
        public string? Name { get; set; } = null;

        [SwaggerSchema(Title = "E5 account number")]
        public string? E5AccountNumber { get; set; } = null;
    }

}

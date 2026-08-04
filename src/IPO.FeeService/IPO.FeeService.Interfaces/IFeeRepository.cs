using IPO.FeeService.Models.API.FeeInformation;
using IPO.FeeService.Models.Constants;
using IPO.FeeService.Models.Data;

namespace IPO.FeeService.Interfaces
{
    public interface IFeeRepository
    {
        Task<ServiceRequestType> GetServiceRequestDetailsAsync(ServiceRequestTypeEnum serviceRequestType);
        Task<List<ProductInformationResult>> GetProductInformation(int[] productNumbers, DateTime effectiveDate, string channel);
        void SeedDatabase(DirectoryInfo directory);
        DirectoryInfo GetSeedDataDirectory(string solutionPath);
    }
}

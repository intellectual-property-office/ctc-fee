using IPO.FeeService.Interfaces;
using IPO.FeeService.Models.API;
using IPO.FeeService.Models.API.FeeInformation;
using IPO.FeeService.Models.Constants;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IPO.FeeService.BDDTests.Mocks
{
    public class MockFeeManagementService : IFeeManagementService
    {
        public Task<FeeCalculationResults> CalculateFeesAsync(ServiceRequestTypeEnum serviceRequestType, FeeCalculationRequest feeCalculationRequest, string channel)
        {
            return Task.FromResult(new FeeCalculationResults(serviceRequestType, new List<FeeItem>(), "", channel));
        }

        public Task<FeeInformationResult> RetrieveFeeInformation(FeeInformationRequest request, string channel)
        {
            return Task.FromResult(new FeeInformationResult("", "00", 0m, 0m, 0m, new List<ProductInformationResult>()));
        }
    }
}

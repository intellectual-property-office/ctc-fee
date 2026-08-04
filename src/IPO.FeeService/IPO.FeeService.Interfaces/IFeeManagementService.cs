using IPO.FeeService.Models.API;
using IPO.FeeService.Models.API.FeeInformation;
using IPO.FeeService.Models.Constants;

namespace IPO.FeeService.Interfaces
{
    public interface IFeeManagementService
    {
        Task<FeeCalculationResults> CalculateFeesAsync(ServiceRequestTypeEnum serviceRequestType, FeeCalculationRequest feeCalculationRequest, string channel);
        Task<FeeInformationResult> RetrieveFeeInformation(FeeInformationRequest request, string channel);
    }
}

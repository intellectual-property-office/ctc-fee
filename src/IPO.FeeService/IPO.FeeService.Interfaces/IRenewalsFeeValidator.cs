using IPO.FeeService.Models.API;
using IPO.FeeService.Models.Validation;

namespace IPO.FeeService.Services.Validation
{
    public interface IRenewalsFeeValidator
    {
        List<RenewalsFeeValidationResult> ValidateRequest(FeeCalculationRequestDetails request);
    }
}

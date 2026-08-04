using IPO.Common.Infrastructure;
using IPO.FeeService.Interfaces;
using IPO.FeeService.Models.API;
using IPO.FeeService.Models.API.FeeInformation;
using IPO.FeeService.Models.Constants;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Swashbuckle.AspNetCore.Filters;
using System.Threading.Tasks;

namespace IPO.FeeService.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FeeController : ControllerBase
    {
        private readonly IFeeManagementService _feeManagementService;
        public FeeController(IFeeManagementService feeManagementService)
        {
            _feeManagementService = feeManagementService;
        }

        /// <summary>
        /// Retrieves the fee for a specified service request type
        /// </summary>
        [SwaggerOperation(
            Summary = "Retrieves the fee (including a breakdown) for a specified service request type",
            Description = "**Notes:** \n\n This is a generic endpoint which handles all service request types. Therefore all parameters are coded as optional, but each service request type has specific parameters without which it will not function. For further details [see the Integration Guide](https://ukipo.visualstudio.com/CTC-Programme/_wiki/wikis/CTC-Programme.wiki/6855/Integration-Guide) on the parameters required for different service request types. \n\n The customer channel is an enum representing the channel of communication with the customer. The possible values are \"00\" (Electronic), \"01\" (Paper), \"10\" (API) and \"02\" (Bulk). The default value is 00. \n\n All date parameters are in the format yyyy-MM-dd e.g. 2023-04-19."
            )]
        [SwaggerRequestExample(typeof(FeeCalculationRequest), typeof(FeeCalculationRequestExamples))]
        [Consumes("application/json")]
        [Produces("application/json")]
        [HttpPost("/calculate/{serviceRequestType}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(FeeCalculationResults))]
        public async Task<ActionResult<FeeCalculationResults>> CalculateFee([FromBody] FeeCalculationRequest request, [FromRoute] ServiceRequestTypeEnum serviceRequestType,[FromQuery] string channel = "00")
        {
            var result = await _feeManagementService.CalculateFeesAsync(serviceRequestType, request, channel);

            return Ok(result);
        }


        /// <summary>
        /// Retrieve all related fee information for a specified channel, effective date & product number(s)
        /// </summary>
        [SwaggerOperation(
            Summary = "Retrieve all related fee information for a specified channel, effective date & product number(s)",
            Description = "**Notes:** \n\n The customer channel is an enum representing the channel of communication with the customer. The possible values are \"00\" (Electronic), \"01\" (Paper), \"10\" (API) and \"02\" (Bulk). The default value is 00. \n\n All date parameters are in the format yyyy-MM-dd e.g. 2023-04-19.")]
        [SwaggerRequestExample(typeof(FeeInformationRequest), typeof(FeeInformationRequestExamples))]
        [Consumes("application/json")]
        [Produces("application/json")]
        [HttpPost("/byProductNumber")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(FeeInformationResult))]
        [ProducesResponseType(typeof(IPOErrorResponse), 404)]
        public async Task<ActionResult<FeeInformationResult>> RetrieveFeeInformation(
            [FromBody] FeeInformationRequest request,
            [FromQuery, SwaggerParameter(Required = false)] string channel = "00")
        {

            var result = await _feeManagementService.RetrieveFeeInformation(request, channel);
            return Ok(result);
        }
    }
}

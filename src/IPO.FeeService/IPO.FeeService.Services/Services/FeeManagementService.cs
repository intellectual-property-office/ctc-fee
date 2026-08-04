using IPO.Common.Infrastructure;
using IPO.FeeService.Interfaces;
using IPO.FeeService.Models.API;
using IPO.FeeService.Models.API.FeeInformation;
using IPO.FeeService.Models.Constants;
using IPO.FeeService.Services.Validation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace IPO.FeeService.Services.Services
{
	public class FeeManagementService : IFeeManagementService
    {
        private IFeeRepository _feeRepository { get; set; }
        private readonly IRenewalsFeeValidator _renewalsFeeValidator;
        private readonly FeeCalculations _feeCalculation;
        private readonly IConfiguration _feeConfiguration;

        public FeeManagementService(IFeeRepository feeRepository, IRenewalsFeeValidator renewalsFeeValidator, IConfiguration feeConfiguration)
        {
            _feeRepository = feeRepository;
            _renewalsFeeValidator = renewalsFeeValidator;
            _feeCalculation = new FeeCalculations(_renewalsFeeValidator);
            _feeConfiguration = feeConfiguration;
        }

        public virtual async Task<FeeCalculationResults> CalculateFeesAsync(ServiceRequestTypeEnum serviceRequestType, FeeCalculationRequest feeCalculationRequest, string channel)
        {
            ValidateChannel(channel);                     
            //Get the effective date
            if (!DateTime.TryParseExact(feeCalculationRequest.PaidDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime paidDate))
            {
                var message = $"The Paid Date value '{feeCalculationRequest.PaidDate}' could not be processed. A valid date must be provided and formatted as yyyy-MM-dd.";
                var error = Error.GetError<FeeManagementService>();
                error.Description = string.IsNullOrEmpty(error.Description) ? message : error.Description + $" {message}";
                throw new StatusCodeException(error, message, null!, StatusCodes.Status422UnprocessableEntity);
            }

            //NOTE: feeCalculationRequest.PaidDate and DateTime paidDate must be identical.                     
            if (CheckDateModifier(out int daysToModify))
            {           
                paidDate = paidDate.AddDays(daysToModify);
                feeCalculationRequest.PaidDate = paidDate.ToString("yyyy-MM-dd");             
            }

            //Get full ServiceRequest record from database
            var serviceRequest = await _feeRepository.GetServiceRequestDetailsAsync(serviceRequestType);
            if (serviceRequest == null)
            {
                var message = $"No matching service request for '{serviceRequestType}' could be found";
                var error = Error.GetError<FeeManagementService>();
                error.Description = string.IsNullOrEmpty(error.Description) ? message : error.Description + $" {message}";
                throw new StatusCodeException(error, message, null!, StatusCodes.Status422UnprocessableEntity);
            }
           
            //Get the account number
            var accountNumber = serviceRequest.E5AccountNumber;

            List<FeeItem> breakdown = new();

            _feeCalculation.CalculateBreakDown(feeCalculationRequest, serviceRequest, channel, paidDate, breakdown, serviceRequestType == ServiceRequestTypeEnum.Renewals);

            var result = new FeeCalculationResults(serviceRequestType, breakdown, accountNumber!, channel);

            return result;
        }



        public async Task<FeeInformationResult> RetrieveFeeInformation(FeeInformationRequest request, string channel)
        {
            //Validate input
            ValidateChannel(channel);
            DateTime effectiveDate = ValidateAndReturnDate(request.EffectiveDate);

            //NOTE: request.EffectiveDate and DateTime effectiveDate must be identical.      
            if (CheckDateModifier(out int daysToModify))
            {
                effectiveDate = effectiveDate.AddDays(daysToModify);
                request.EffectiveDate = effectiveDate.ToString("yyyy-MM-dd");
            }

            var productNumbers = ValidateProductNumbers(request.ProductNumbers);

            //Get Data
            var productInformation = await _feeRepository.GetProductInformation(productNumbers, effectiveDate, channel);

            //Validate output
            ValidateAllProductNumbersExist(productInformation, productNumbers);
            productInformation = FilterProductsByChannelAndValidate(productInformation, productNumbers, channel);
            productInformation = FilterProductsByDateAndValidate(productInformation, productNumbers, effectiveDate);

            //Format output
            var feeInformationResult = new FeeInformationResult(
                request.EffectiveDate, 
                channel, 
                productInformation.Sum(f => f.RecentExVat), 
                productInformation.Sum(f => f.RecentVat), 
                productInformation.Sum(f => f.RecentIncVat), 
                productInformation);


            return feeInformationResult;
        }


        internal void ValidateAllProductNumbersExist(List<ProductInformationResult> productInformation, int[] productNumbers, string errorMessage = "The following product numbers were not found:")
        {
            var missing = productNumbers
                    .Where(num => !productInformation.Any(pi => pi.Number == num))
                    .ToList();

            if (missing.Any())
            {
                var message = errorMessage + $" {string.Join(", ", missing)}";
                var error = Error.GetError<FeeManagementService>();
                error.Description = string.IsNullOrEmpty(error.Description) ? message : error.Description + $" {message}";
                throw new StatusCodeException(error, message, null!, StatusCodes.Status404NotFound);
            }
        }

        internal List<ProductInformationResult> FilterProductsByChannelAndValidate(List<ProductInformationResult> productInformation, int[] productNumbers, string channel)
        {
            var returnList = productInformation.Where(pi => pi.Channel == channel).ToList();

            ValidateAllProductNumbersExist(returnList, productNumbers, $"The following product numbers have no fee, quantity rule, or tax code applicable for channel '{channel}':");

            return returnList;
        }

        internal List<ProductInformationResult> FilterProductsByDateAndValidate(List<ProductInformationResult> productInformation, int[] productNumbers, DateTime effectiveDate)
        {
            var returnList = productInformation
                .Select(pi =>
                {
                    var filteredDetails = pi.Details
                        .Where(d =>
                            effectiveDate >= d.Fee.EffectiveFrom &&
                            (d.Fee.EffectiveTo == null || effectiveDate <= d.Fee.EffectiveTo)
                        ||
                            effectiveDate >= d.QuantityRules.EffectiveFrom &&
                            (d.QuantityRules.EffectiveTo == null || effectiveDate <= d.QuantityRules.EffectiveTo)
                        ||
                            effectiveDate >= d.TaxCode.EffectiveFrom &&
                            (d.TaxCode.EffectiveTo == null || effectiveDate <= d.TaxCode.EffectiveTo))
                        .ToList();

                    return new ProductInformationResult(
                        pi.Number,
                        pi.Channel,
                        pi.Name,
                        pi.E5ProductCode,
                        pi.RecentExVat,
                        pi.RecentVat,
                        filteredDetails
                    );
                })
                .Where(pi => pi.Details.Any())
                .ToList();

            ValidateAllProductNumbersExist(returnList, productNumbers, $"The following product numbers have no fee, quantity rule, or tax code applicable on effective date '{effectiveDate:d}':");

            return returnList;
        }




        internal void ValidateChannel(string channel)
		{
			if (!Settings.SupportedChannels.Contains(channel))
			{
				var message = $"The customer channel '{channel}' is not supported";
				var error = Error.GetError<FeeManagementService>();
				error.Description = string.IsNullOrEmpty(error.Description) ? message : error.Description + $" {message}";
				throw new StatusCodeException(error, message, null!, StatusCodes.Status422UnprocessableEntity);
			}
		}

        internal DateTime ValidateAndReturnDate(string date)
        {
            if (!DateTime.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedDate))
            {
                var message = $"The Effective Date value '{date}' could not be processed. A valid date must be provided and formatted as yyyy-MM-dd.";
                var error = Error.GetError<FeeManagementService>();
                error.Description = string.IsNullOrEmpty(error.Description) ? message : error.Description + $" {message}";
                throw new StatusCodeException(error, message, null!, StatusCodes.Status422UnprocessableEntity);
            }
            return parsedDate;
        }

        internal int[] ValidateProductNumbers(int[]? productNumbers)
        {
            if(productNumbers == null || productNumbers.Length == 0)
            {
                var message = $"No Product Numbers have been provided. A valid integer array must be provided.";
                var error = Error.GetError<FeeManagementService>();
                error.Description = string.IsNullOrEmpty(error.Description) ? message : error.Description + $" {message}";
                throw new StatusCodeException(error, message, null!, StatusCodes.Status422UnprocessableEntity);
            }


            var invalid = productNumbers.Where(n => n <= 0).ToList();
            if (invalid.Any())
            {
                var message = $"The Product Numbers '{string.Join(", ", invalid)}' could not be processed. A valid integer array must be provided.";
                var error = Error.GetError<FeeManagementService>();
                error.Description = string.IsNullOrEmpty(error.Description) ? message : error.Description + $" {message}";
                throw new StatusCodeException(error, message, null!, StatusCodes.Status422UnprocessableEntity);
            }

            return productNumbers;
        }

        internal bool CheckDateModifier(out int daysToModify)
        {
            //'DaysToModifyDate' environment variable value should always be 0 unless in use
            var config = _feeConfiguration;
            daysToModify = int.TryParse(config["DaysToModifyDate"], out var value) ? value : 0;
                  
            return daysToModify != 0;

        }
    }
}

using IPO.Common.Infrastructure;
using IPO.FeeService.Models.API;
using IPO.FeeService.Models.Validation;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace IPO.FeeService.Services.Validation
{
	public class RenewalsFeeValidator : IRenewalsFeeValidator
    {
        public List<RenewalsFeeValidationResult> ValidateRequest(FeeCalculationRequestDetails details)
        {
            var validationResults = new List<RenewalsFeeValidationResult>();

            if (details.RightId == null)
                validationResults.Add(RenewalsFeeValidationResult.CreateIsRequiredFieldValidationResult("rightId"));

            if (string.IsNullOrWhiteSpace(details.RightType))
                validationResults.Add(RenewalsFeeValidationResult.CreateIsRequiredFieldValidationResult("rightType"));

            if (string.IsNullOrWhiteSpace(details.PatentType))
                validationResults.Add(RenewalsFeeValidationResult.CreateIsRequiredFieldValidationResult("patentType"));

            if (details.FromRenewalYear == null)
                validationResults.Add(RenewalsFeeValidationResult.CreateIsRequiredFieldValidationResult("fromRenewalYear"));

            if (details.RenewalYear == null)
                validationResults.Add(RenewalsFeeValidationResult.CreateIsRequiredFieldValidationResult("renewalYear"));

            if (details.IsLateGrant == null)
                validationResults.Add(RenewalsFeeValidationResult.CreateIsRequiredFieldValidationResult("isLateGrant"));

            if (details.IsLOR == null)
                validationResults.Add(RenewalsFeeValidationResult.CreateIsRequiredFieldValidationResult("isLOR"));

			if (!DateTime.TryParseExact(details?.RenewalDueDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
			{
				validationResults.Add(RenewalsFeeValidationResult.CreateDateFormatValidationResult("Renewal Due Date", details!.RenewalDueDate!));
			}

            if (!DateTime.TryParseExact(details?.PaymentDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
            {
                validationResults.Add(RenewalsFeeValidationResult.CreateDateFormatValidationResult("Payment Date", details!.PaymentDate!));
            }

            return validationResults;
        }

        public static StatusCodeExceptionList GetStatusCodeExceptionList<T>(int code, string errorCode, List<RenewalsFeeValidationResult> validationResults)
        {
            var errors = new List<Error>();
            foreach (var result in validationResults)
            {
                var error = Error.Create<T>(errorCode);
                error.Description += $" {result.ErrorMessage}";
                errors.Add(error);
            }
    
            return new StatusCodeExceptionList(errors, code);
        }
    }
}

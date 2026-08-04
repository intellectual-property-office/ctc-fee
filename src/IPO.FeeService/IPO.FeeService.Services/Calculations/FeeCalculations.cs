using IPO.Common.Infrastructure;
using IPO.FeeService.Models.API;
using IPO.FeeService.Models.Constants;
using IPO.FeeService.Models.Data;
using IPO.FeeService.Services.Calculations;
using IPO.FeeService.Services.Services;
using IPO.FeeService.Services.Validation;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;

namespace IPO.FeeService.Services
{
    public class FeeCalculations : CommonFeeCalculations
    {
        private readonly IRenewalsFeeValidator _renewalsFeeValidator;

        public FeeCalculations(IRenewalsFeeValidator renewalsFeeValidator)
        {
            _renewalsFeeValidator = renewalsFeeValidator;
        }

        internal int MonthsLate { get; set; }

 		internal override void CalculateBreakDown(FeeCalculationRequest feeCalculationRequest, ServiceRequestType serviceRequest, string channel, DateTime paidDate, List<FeeItem> breakdown, bool isRenewals)
		{
            //NOTE: feeCalculationRequest.PaidDate and DateTime paidDate must be identical

            //Create an empty request details if the list is null or empty
            var requestDetailsList = feeCalculationRequest.RequestDetails?.ToList();
            
			if (requestDetailsList == null || requestDetailsList.Count == 0)
			{
				requestDetailsList = new() { new FeeCalculationRequestDetails() };
				feeCalculationRequest.RequestDetails = requestDetailsList.ToArray();
			}
            
			foreach (var details in feeCalculationRequest.RequestDetails!)
			{
                if (isRenewals)
                {
                    details.PaidDate = feeCalculationRequest.PaidDate;
                    details.PaymentDate = string.IsNullOrEmpty(details.PaymentDate) ? details.PaidDate : details.PaymentDate;
                    var validationResults = _renewalsFeeValidator.ValidateRequest(details);
                    if (validationResults.Any(a => a.Code != 200))
                    {
                        throw RenewalsFeeValidator.GetStatusCodeExceptionList<RenewalsFeeValidator>(422, "E006", validationResults);
                    }
                    DateTime renewalDueDate = DateTime.ParseExact(details?.RenewalDueDate!, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None);
                    MonthsLate = CalculateMonthsLate(renewalDueDate, paidDate);
                }

				//Fetch products from the database by service request type and filter by channel, sort results by ProductSequence
				var sortedProducts = serviceRequest.ProductServiceRequestTypes
	                .Where(x => x.Product!.Channel == channel)
	                .OrderBy(x => x.ProductSequence)
	                .Select(x => new
	                {
		                x.Product,
		                x.ProductSequence,
		                x.Product!.Fees,
		                x.Product.QuantityRules,
		                x.Product.TaxCodes
	                })
	                .ToList();

				int lineNumber = 1;
				foreach (var product in sortedProducts)
				{
					var fees = product.Fees!
						.Where(x => paidDate >= x.EffectiveFrom && (x.EffectiveTo == null || paidDate <= x.EffectiveTo))
						.ToList();

					var quantityRule = product.QuantityRules.FirstOrDefault(x => paidDate >= x.EffectiveFrom && (x.EffectiveTo == null || paidDate <= x.EffectiveTo));

					var taxCode = product.TaxCodes.FirstOrDefault(x => paidDate >= x.EffectiveFrom && (x.EffectiveTo == null || paidDate <= x.EffectiveTo));

					if (product is null || fees is null || fees.Count == 0 || quantityRule is null || taxCode is null)
					{
						var message = $"Unable to find data for the given request. A fee could not be calculated.";
						var error = Error.GetError<FeeManagementService>();
						error.Description = string.IsNullOrEmpty(error.Description) ? message : error.Description + $" {message}";
						throw new StatusCodeException(error, message, null!, StatusCodes.Status404NotFound);
					}

					var quantity = CalculateQuantityForProduct(details!, quantityRule);

					var rightId = details!.RightId ?? 0;

					if (quantity == 0)
						continue;

					var fee = ProcessFeeRules(details, fees);
					if (fee is not null)
					{
						AddNewLineItem(breakdown, ref lineNumber, product.Product!, fee, taxCode, quantity, rightId, isRenewals, product.ProductSequence);
					}
				}
			}
			SortFeeItems(breakdown, serviceRequest, channel);
		}

		internal Fee? ProcessFeeRules(FeeCalculationRequestDetails details, List<Fee> fees)
		{
			var effectiveFees = fees.Where(fee => EvaluateFeeRule(details, fee.Rule)).ToList();

			return effectiveFees.Count switch
			{
				0 => null,
				1 => effectiveFees[0],
				_ => throw CreateFeeAmbiguityException()
			};

			StatusCodeException CreateFeeAmbiguityException()
			{
				var message = "Unable to find a single fee for the given request. A fee could not be calculated.";
				var error = Error.GetError<FeeManagementService>();
				error.Description = string.IsNullOrEmpty(error.Description) ? message : $"{error.Description} {message}";
				return new StatusCodeException(error, message, null, StatusCodes.Status422UnprocessableEntity);
			}
		}

		public void SortFeeItems(List<FeeItem> breakdown, ServiceRequestType serviceRequest, string channel)
		{
			breakdown.Sort((x, y) => x.ProductSequence.CompareTo(y.ProductSequence));

            for (int i = 0; i < breakdown.Count; i++)
            {
				breakdown[i].LineNum = i + 1;
            }

            return;
        }

		private void AddNewLineItem(List<FeeItem> breakdown, ref int lineNumber, Product product, Fee fee, TaxCode taxCode, int quantity, int rightId, bool isRenewals, int productSequence)
        {
			var existingItem = breakdown.FirstOrDefault(x => x.ProductSequence == productSequence && x.Price == fee.PriceExVat);

			if (existingItem != null)
            {
                existingItem.Quantity += quantity;
                existingItem.LineTotal = existingItem.Price * existingItem.Quantity;
                existingItem.VatAmount = decimal.Round(existingItem.LineTotal * taxCode.Rate, 2);

                if (isRenewals) CreateOrUpdateRightsQuantity(existingItem, quantity, rightId);
            }
            else
            {
                var newLineItem = CalculateLineItem(lineNumber, product, fee, taxCode, quantity, isRenewals, productSequence);

				if (isRenewals) CreateOrUpdateRightsQuantity(newLineItem, quantity, rightId);

                breakdown.Add(newLineItem);
                lineNumber++;
            }
        }

        internal override FeeItem CalculateLineItem(int lineNumber, Product product, Fee fee, TaxCode taxCode, int quantity, bool isRenewals, int productSequence)
        {
            decimal lineTotal = fee.PriceExVat * quantity;
            decimal vatAmount = decimal.Round(lineTotal * taxCode.Rate, 2); //Actually we need to convert this rate to a percentage...

            return new FeeItem()
            {
                LineNum = lineNumber,
                ProductName = product.Description,
                E5ProductCode = product.Code,
                Quantity = quantity,
                Price = fee.PriceExVat,
                LineTotal = lineTotal,
                VatCode = taxCode.Code,
                VatAmount = vatAmount,
				ProductSequence = productSequence,
				ProductDetails = isRenewals ? new RightIdsByQuantityBreakdown()
				{
					RightIdsByQuantity = Array.Empty<RenewalRightsQuantityItem>()
				} :
				new object() { } //empty object for service requests that are not renewals
			};
        }

		#region Renewals only methods
		private void CreateOrUpdateRightsQuantity(FeeItem lineItem, int quantity, int rightId)
        {
            if (lineItem.ProductDetails is not RightIdsByQuantityBreakdown)
            {
                lineItem.ProductDetails = new RightIdsByQuantityBreakdown() 
                { 
                    RightIdsByQuantity = Array.Empty<RenewalRightsQuantityItem>()
                };
            }

            var rightsQuantityItems = lineItem.ProductDetails as RightIdsByQuantityBreakdown; // List<RenewalRightsQuantityItem>;

            var item = rightsQuantityItems!.RightIdsByQuantity!.FirstOrDefault(x => x.Quantity == quantity);
            if (item != null)
            {
                item.RightIds!.Add(rightId);
            }
            else
            {
                var rightIdsByQuantityAsList = rightsQuantityItems.RightIdsByQuantity!.ToList();
                
                rightIdsByQuantityAsList.Add(new RenewalRightsQuantityItem
                {
                    Quantity = quantity,
                    RightIds = new List<int> { rightId }
                });

                rightsQuantityItems.RightIdsByQuantity = rightIdsByQuantityAsList.ToArray();
            }
        }

        private int CalculateMonthsLate(DateTime dueDate, DateTime paidDate)
        {
            // NOTE: Do we need to throw error on payments made too early? No we don't. It's not a requirement, but leaving the checking code here in case we ever need it.
            //var earliestPermittedDate = GetAdjustedStartOfMonthDate(dueDate.Year, dueDate.Month, -2);
            //if (paidDate < earliestPermittedDate)
            //{
            //    var message = $"The Paid Date value '{paidDate}' is more than two months earlier than the due date.";
            //    var error = Error.GetError<FeeManagementService>();
            //    error.Description = string.IsNullOrEmpty(error.Description) ? message : error.Description + $" {message}";
            //    throw new StatusCodeException(error, message, null, StatusCodes.Status406NotAcceptable);
            //}

            int monthsLate = 0;
            var checkDate = GetAdjustedEndOfMonthDate(dueDate.Year, dueDate.Month, 0);
            while (paidDate > checkDate)// && monthsLate <= 6)
            {
                monthsLate++;
                checkDate = GetAdjustedEndOfMonthDate(dueDate.Year, dueDate.Month, monthsLate);
            }

            // NOTE: Apparently we don't need to throw an exception here, but leaving the handling code commented out just in case we need it later.
            //if (monthsLate > 6)
            //{
            //    var message = $"The Paid Date value '{paidDate}' is more than six months later than the due date.";
            //    var error = Error.GetError<FeeManagementService>();
            //    error.Description = string.IsNullOrEmpty(error.Description) ? message : error.Description + $" {message}";
            //    throw new StatusCodeException(error, message, null, StatusCodes.Status406NotAcceptable);
            //}

            return monthsLate;
        }

        [ExcludeFromCodeCoverage]
        private DateTime GetAdjustedStartOfMonthDate(int year, int month, int monthsLate)
        {
            //Calculate the new date
            var newDate = new DateTime(year, month, 1).AddMonths(monthsLate);
            var startOfMonth = new DateTime(newDate.Year, newDate.Month, 1);

            //No need to check for non-working days

            return startOfMonth;
        }

        private DateTime GetAdjustedEndOfMonthDate(int year, int month, int monthsLate)
        {
            //Calculate the new date
            var newDate = new DateTime(year, month, 1).AddMonths(monthsLate);
            var lastDayOfMonth = DateTime.DaysInMonth(newDate.Year, newDate.Month);
            var endOfMonth = new DateTime(newDate.Year, newDate.Month, lastDayOfMonth).AddDays(1).AddSeconds(-1);

            //Now check if last day of month is non-working day, and update if necessary
            endOfMonth = AdjustForNonWorkingDays(endOfMonth);

            return endOfMonth;
        }

        internal DateTime AdjustForNonWorkingDays(DateTime date)
        {
            var adjustedDate = date;

            var nonWorkingDaysAtIPO = FetchIPONonWorkingDays();

            bool isNonWorkingDay = nonWorkingDaysAtIPO.Any(x => x.Date.Year == date.Year && x.Date.Month == date.Month && x.Date.Day == date.Day);

            while (isNonWorkingDay)
            {
                adjustedDate = adjustedDate.AddDays(1);
                isNonWorkingDay = nonWorkingDaysAtIPO.Any(x => x.Date.Year == adjustedDate.Year && x.Date.Month == adjustedDate.Month && x.Date.Day == adjustedDate.Day);
            }

            return adjustedDate;
        }

        //TODO: Replace this with an API
        private List<NonWorkingDay> FetchIPONonWorkingDays()
        {
            //First add bank holidays
            var nonWorkingDays = new List<NonWorkingDay>
            {
                new NonWorkingDay(new DateTime(2020, 01, 01), NonWorkingDayType.BankHoliday, ""),
                new NonWorkingDay(new DateTime(2020, 04, 10), NonWorkingDayType.BankHoliday, ""),
                new NonWorkingDay(new DateTime(2020, 04, 13), NonWorkingDayType.BankHoliday, ""),
                new NonWorkingDay(new DateTime(2020, 05, 08), NonWorkingDayType.BankHoliday, ""),
                new NonWorkingDay(new DateTime(2020, 05, 25), NonWorkingDayType.BankHoliday, ""),
                new NonWorkingDay(new DateTime(2020, 08, 31), NonWorkingDayType.BankHoliday, ""),
                new NonWorkingDay(new DateTime(2020, 12, 25), NonWorkingDayType.BankHoliday, ""),
                new NonWorkingDay(new DateTime(2020, 12, 28), NonWorkingDayType.BankHoliday, ""),
                new NonWorkingDay(new DateTime(2021, 01, 01), NonWorkingDayType.BankHoliday, ""),
                new NonWorkingDay(new DateTime(2021, 04, 02), NonWorkingDayType.BankHoliday, ""),
                new NonWorkingDay(new DateTime(2021, 04, 05), NonWorkingDayType.BankHoliday, ""),
                new NonWorkingDay(new DateTime(2021, 05, 03), NonWorkingDayType.BankHoliday, ""),
                new NonWorkingDay(new DateTime(2021, 05, 31), NonWorkingDayType.BankHoliday, ""),
                new NonWorkingDay(new DateTime(2021, 08, 30), NonWorkingDayType.BankHoliday, ""),
                new NonWorkingDay(new DateTime(2021, 12, 27), NonWorkingDayType.BankHoliday, ""),
                new NonWorkingDay(new DateTime(2021, 12, 28), NonWorkingDayType.BankHoliday, ""),
                new NonWorkingDay(new DateTime(2022, 01, 03), NonWorkingDayType.BankHoliday, ""),
                new NonWorkingDay(new DateTime(2022, 04, 15), NonWorkingDayType.BankHoliday, ""),
                new NonWorkingDay(new DateTime(2022, 04, 18), NonWorkingDayType.BankHoliday, ""),
                new NonWorkingDay(new DateTime(2022, 05, 02), NonWorkingDayType.BankHoliday, ""),
                new NonWorkingDay(new DateTime(2022, 06, 02), NonWorkingDayType.BankHoliday, "Queen's Jubilee"),
                new NonWorkingDay(new DateTime(2022, 06, 03), NonWorkingDayType.BankHoliday, "Queen's Jubilee"),
                new NonWorkingDay(new DateTime(2022, 08, 29), NonWorkingDayType.BankHoliday, ""),
                new NonWorkingDay(new DateTime(2022, 09, 19), NonWorkingDayType.BankHoliday, "Queen's Funeral"),
                new NonWorkingDay(new DateTime(2022, 12, 26), NonWorkingDayType.BankHoliday, ""),
                new NonWorkingDay(new DateTime(2022, 12, 27), NonWorkingDayType.BankHoliday, ""),
                new NonWorkingDay(new DateTime(2023, 01, 02), NonWorkingDayType.BankHoliday, ""),
                new NonWorkingDay(new DateTime(2023, 04, 07), NonWorkingDayType.BankHoliday, ""),
                new NonWorkingDay(new DateTime(2023, 04, 10), NonWorkingDayType.BankHoliday, ""),
                new NonWorkingDay(new DateTime(2023, 05, 01), NonWorkingDayType.BankHoliday, ""),
                new NonWorkingDay(new DateTime(2023, 05, 08), NonWorkingDayType.BankHoliday, "King's Coronation"),
                new NonWorkingDay(new DateTime(2023, 05, 29), NonWorkingDayType.BankHoliday, ""),
                new NonWorkingDay(new DateTime(2023, 08, 28), NonWorkingDayType.BankHoliday, ""),
                new NonWorkingDay(new DateTime(2023, 12, 25), NonWorkingDayType.BankHoliday, ""),
                new NonWorkingDay(new DateTime(2023, 12, 26), NonWorkingDayType.BankHoliday, ""),
                new NonWorkingDay(new DateTime(2024, 01, 01), NonWorkingDayType.BankHoliday, ""),
                new NonWorkingDay(new DateTime(2024, 03, 29), NonWorkingDayType.BankHoliday, ""),
                new NonWorkingDay(new DateTime(2024, 04, 01), NonWorkingDayType.BankHoliday, ""),
                new NonWorkingDay(new DateTime(2024, 05, 06), NonWorkingDayType.BankHoliday, ""),
                new NonWorkingDay(new DateTime(2024, 05, 27), NonWorkingDayType.BankHoliday, ""),
                new NonWorkingDay(new DateTime(2024, 08, 26), NonWorkingDayType.BankHoliday, ""),
                new NonWorkingDay(new DateTime(2024, 12, 25), NonWorkingDayType.BankHoliday, ""),
                new NonWorkingDay(new DateTime(2024, 12, 26), NonWorkingDayType.BankHoliday, ""),
                new NonWorkingDay(new DateTime(2025, 01, 01), NonWorkingDayType.BankHoliday, ""),
                new NonWorkingDay(new DateTime(2025, 04, 18), NonWorkingDayType.BankHoliday, ""),
                new NonWorkingDay(new DateTime(2025, 04, 21), NonWorkingDayType.BankHoliday, ""),
                new NonWorkingDay(new DateTime(2025, 05, 05), NonWorkingDayType.BankHoliday, ""),
                new NonWorkingDay(new DateTime(2025, 05, 26), NonWorkingDayType.BankHoliday, ""),
                new NonWorkingDay(new DateTime(2025, 08, 25), NonWorkingDayType.BankHoliday, ""),
                new NonWorkingDay(new DateTime(2025, 12, 25), NonWorkingDayType.BankHoliday, ""),
                new NonWorkingDay(new DateTime(2025, 12, 26), NonWorkingDayType.BankHoliday, ""),
                new NonWorkingDay(new DateTime(2026, 01, 01), NonWorkingDayType.BankHoliday, ""),
                new NonWorkingDay(new DateTime(2026, 04, 03), NonWorkingDayType.BankHoliday, ""),
                new NonWorkingDay(new DateTime(2026, 04, 06), NonWorkingDayType.BankHoliday, ""),
                new NonWorkingDay(new DateTime(2026, 05, 04), NonWorkingDayType.BankHoliday, ""),
                new NonWorkingDay(new DateTime(2026, 05, 25), NonWorkingDayType.BankHoliday, ""),
                new NonWorkingDay(new DateTime(2026, 08, 31), NonWorkingDayType.BankHoliday, ""),
                new NonWorkingDay(new DateTime(2026, 12, 25), NonWorkingDayType.BankHoliday, ""),
                new NonWorkingDay(new DateTime(2026, 12, 28), NonWorkingDayType.BankHoliday, ""),
                new NonWorkingDay(new DateTime(2027, 01, 01), NonWorkingDayType.BankHoliday, ""),
                new NonWorkingDay(new DateTime(2027, 03, 26), NonWorkingDayType.BankHoliday, ""),
                new NonWorkingDay(new DateTime(2027, 03, 29), NonWorkingDayType.BankHoliday, ""),
                new NonWorkingDay(new DateTime(2027, 05, 03), NonWorkingDayType.BankHoliday, ""),
                new NonWorkingDay(new DateTime(2027, 05, 31), NonWorkingDayType.BankHoliday, ""),
                new NonWorkingDay(new DateTime(2027, 08, 30), NonWorkingDayType.BankHoliday, ""),
                new NonWorkingDay(new DateTime(2027, 12, 27), NonWorkingDayType.BankHoliday, ""),
                new NonWorkingDay(new DateTime(2027, 12, 28), NonWorkingDayType.BankHoliday, "")
            };

            //Now add weekends
            DateTime iteratingDate = new(2020, 01, 01);
            DateTime limitDate = new(2028, 01, 01);
            while (iteratingDate < limitDate)
            {
                if (iteratingDate.DayOfWeek == DayOfWeek.Saturday)
                {
                    nonWorkingDays.Add(new(iteratingDate, NonWorkingDayType.Weekend, "Saturday"));
                }
                else if (iteratingDate.DayOfWeek == DayOfWeek.Sunday)
                {
                    nonWorkingDays.Add(new(iteratingDate, NonWorkingDayType.Weekend, "Sunday"));
                }

                iteratingDate = iteratingDate.AddDays(1);
            }

            return nonWorkingDays.OrderBy(x => x.Date).ToList();
        }

        internal override object ApplySpecialRules(string ruleInTitleCase)
        {
            if (ruleInTitleCase == "#MonthsLate")
            {
                return MonthsLate;
            }
            else
            {
                return null!;
            }
        }
		#endregion
	}
}

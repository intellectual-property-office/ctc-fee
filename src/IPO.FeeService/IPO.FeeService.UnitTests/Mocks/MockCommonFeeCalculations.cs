using IPO.FeeService.Models.API;
using IPO.FeeService.Models.Data;
using IPO.FeeService.Services.Calculations;
using System;
using System.Collections.Generic;

namespace IPO.FeeService.UnitTests.Mocks
{
	public class MockCommonFeeCalculations : CommonFeeCalculations
	{
		public MockCommonFeeCalculations(){}
		internal override void CalculateBreakDown(FeeCalculationRequest feeCalculationRequest, ServiceRequestType serviceRequest, string channel, DateTime paidDate, List<FeeItem> breakdown, bool isRenewals)
		{
			throw new NotImplementedException();
		}

		internal override FeeItem CalculateLineItem(int lineNumber, Product product, Fee fee, TaxCode taxCode, int quantity, bool isRenewals, int productSequence)
		{
			throw new NotImplementedException();
		}
	}
}

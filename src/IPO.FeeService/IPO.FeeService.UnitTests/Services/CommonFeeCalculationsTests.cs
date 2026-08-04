using AwesomeAssertions;
using IPO.FeeService.Models.API;
using IPO.FeeService.Models.Constants;
using IPO.FeeService.UnitTests.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IPO.FeeService.UnitTests.Services
{
	[TestClass]
	public class CommonFeeCalculationsTests
	{
		private readonly MockCommonFeeCalculations _commonFeeCalculations = new MockCommonFeeCalculations();

		[TestMethod]
		[DataRow(true,  Settings.NoFeeRuleValue, 0)]
		[DataRow(false, "MAX(0,numberOfClaims - 25)", 0 )]
		[DataRow(false, "MAX(0,numberOfClaims - 25)", 25)]
		[DataRow(true,  "MAX(0,numberOfClaims - 25)", 26)]
		[DataRow(false, "IF( numberOfClaims >= 1, 1, 0)", 0)]
		[DataRow(true,  "IF( numberOfClaims >= 1, 1, 0)", 1)]
		public void EvaluateFeeRule_ShouldReturnExpectedResult_WhenGivenVariousInputs(bool expectedResult, string rule, int numberOfClaims)
		{
			// Arrange
			var details = new FeeCalculationRequestDetails() { NumberOfClaims = numberOfClaims};

			// Act
			var result = _commonFeeCalculations.EvaluateFeeRule(details, rule);

			// Assert
			result.Should().Be(expectedResult);
		}
	}
}

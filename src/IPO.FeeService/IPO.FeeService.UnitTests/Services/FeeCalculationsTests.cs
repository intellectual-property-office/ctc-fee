using AwesomeAssertions;
using IPO.Common.Infrastructure;
using IPO.FeeService.Data;
using IPO.FeeService.Models.API;
using IPO.FeeService.Models.Constants;
using IPO.FeeService.Models.Data;
using IPO.FeeService.Services;
using IPO.FeeService.Services.Services;
using IPO.FeeService.Services.Validation;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IPO.FeeService.UnitTests.Services
{
	[TestClass]
	public class FeeCalculationsTests
	{
		private readonly FeeDbRepository _feeDbRepository;
		private readonly IRenewalsFeeValidator _renewalsFeeValidator;
		private readonly FeeCalculations _feeCalculation;
		private const string ProductCodeA = "PA";
		private const string ProductCodeB = "PB";
		private const string ProductCodeC = "PC";
		private readonly Mock<ILogger<FeeDbRepository>> _mockLogger;

		public FeeCalculationsTests()
		{
			_mockLogger = new Mock<ILogger<FeeDbRepository>>();
			_feeDbRepository = new FeeDbRepository(new FeeDbContext(
						new DbContextOptionsBuilder<FeeDbContext>()
						.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
						.ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
						.Options
						),
						_mockLogger.Object);
			_renewalsFeeValidator = new RenewalsFeeValidator();
			_feeCalculation = new FeeCalculations(_renewalsFeeValidator);
		}

		#region SortFeeItems
		[TestMethod]
		[DataRow("00", 1, 2, 3, ProductCodeA, ProductCodeB, ProductCodeC)]
		[DataRow("00", 3, 1, 2, ProductCodeB, ProductCodeC, ProductCodeA)]
		[DataRow("01", 3, 2, 1, ProductCodeC, ProductCodeB, ProductCodeA)]
		public async Task SortFeeItems_ShouldReturnSortedBreakdownWithSequentialLineNums_FromUnsortedBreakdown(string channel, int productASequence, int productBSequence, int productCSequence, string expectedProduct1, string expectedProduct2, string expectedProduct3)
		{
			// Arrange
			SetupServiceRequestAndProductWithProductSequence(channel, productASequence, productBSequence, productCSequence);
			var serviceRequest = await _feeDbRepository.GetServiceRequestDetailsAsync(ServiceRequestTypeEnum.PatentApplicationSearchAndExam);
			//var feeCalculations = new FeeCalculations(null!);
			var breakdown = new List<FeeItem>
			{
				new FeeItem { E5ProductCode = ProductCodeA, ProductSequence = productASequence, LineNum = 1 },
				new FeeItem { E5ProductCode = ProductCodeB, ProductSequence = productBSequence, LineNum = 1 },
				new FeeItem { E5ProductCode = ProductCodeC, ProductSequence = productCSequence, LineNum = 1 }
			};

			// Act
			 _feeCalculation.SortFeeItems(breakdown, serviceRequest, channel);

			// Assert
			breakdown.Should().NotBeNull();
			Assert.HasCount(3, breakdown);
			Assert.AreEqual(expectedProduct1, breakdown[0].E5ProductCode);
			Assert.AreEqual(expectedProduct2, breakdown[1].E5ProductCode);
			Assert.AreEqual(expectedProduct3, breakdown[2].E5ProductCode);
			Assert.AreEqual(1, breakdown[0].LineNum);
			Assert.AreEqual(2, breakdown[1].LineNum);
			Assert.AreEqual(3, breakdown[2].LineNum);
		}
        #endregion

        #region AdjustForNonWorkingDays
        [TestMethod]
		[DataRow("2026, 6, 01")] //Monday Working Day
        [DataRow("2026, 6, 02")] //Tuesday Working Day
        [DataRow("2026, 6, 03")] //Wednesday Working Day
        [DataRow("2026, 6, 04")] //Thursday Working Day
        [DataRow("2026, 6, 05")] //Friday Working Day
        public void IsNonWorkingDayFalse_NoDateAdjustment_ReturnsOriginalDate(string dateString)
        {
            //Arrange
            var date = DateTime.Parse(dateString);

            //Act
            var result = _feeCalculation.AdjustForNonWorkingDays(date);

			//Assert			
			result.Should().Be(date);
        }

        [TestMethod]
        [DataRow("2025, 12, 25", "2025-12-29")] //Thursday Long Bank Holiday -> Following Next Working Day (Monday)
        [DataRow("2026, 1, 01", "2026-01-02")] //Thursday Bank Holiday -> Next Day Friday  
        [DataRow("2026, 5, 16", "2026-05-18")] //Saturday Weekend Day -> Following Monday
        [DataRow("2026, 5, 17", "2026-05-18")] //Sunday Weekend Day -> Next Day Monday
        [DataRow("2026, 10, 31", "2026-11-02")] //Saturday Weekend Day -> Following Monday In Next Month
        [DataRow("2026, 12, 25", "2026-12-29")] //Friday Long Bank Holiday -> Following Next Working Day (Tuesday)
        [DataRow("2027, 12, 25", "2027-12-29")] //Saturday Long Bank Holiday -> Following Next Working Day (Wednesday)
        public void IsNonWorkingDayTrue_DateAdjusted_ReturnsNextWorkingDate(string dateString, string expectedDateString)
        {
            //Arrange
            var date = DateTime.Parse(dateString);
            var expectedDate = DateTime.Parse(expectedDateString);

            //Act
            var result = _feeCalculation.AdjustForNonWorkingDays(date);

            //Assert			
            result.Should().NotBe(date);
            result.Should().Be(expectedDate);
        }
		#endregion

		#region ProcessFeeRules
		[TestMethod]
		[DataRow(true, "2025-10-01", "2025-09-01")] // 2 fees match
		public void ProcessFeeRules_ThrowsStatusCodeException_WhenMoreThanOneFeeFound(bool isLor, string paidDate, string renewalDueDate)
		{
			// Arrange
			Error.Add(Error.Create<FeeManagementService>("E005"));
			var ExpectedErrorMessage = $"Unable to find a single fee for the given request. A fee could not be calculated.";

			FeeCalculationRequestDetails details = new FeeCalculationRequestDetails() { IsLOR = isLor, PaidDate = paidDate, RenewalDueDate = renewalDueDate };
			List<Fee> fees = new List<Fee>
			{
				new Fee
				{
					Id = 1,
					Rule = "IF((isLOR=1)*(MIN(paidDate,renewalDueDate)>=\"06/04/2018\"),1,0)"
				},
				new Fee
				{
					Id = 2,
					Rule = "IF((isLOR=0)*(MIN(paidDate,renewalDueDate)>=\"06/04/2018\"),1,0)"
				},
				new Fee
				{
					Id = 3,
					Rule = "IF((isLOR=1)*(MIN(paidDate,renewalDueDate)>=\"06/04/2025\"),1,0)"
				}
			};

			// Act & Assert
			StatusCodeException ex = Assert.ThrowsExactly<StatusCodeException>(() => _feeCalculation.ProcessFeeRules(details, fees));

			// Assert
			ex.Message.Should().Be(ExpectedErrorMessage);
			ex.StatusCode.Should().Be(StatusCodes.Status422UnprocessableEntity);
		}

		[TestMethod]
		[DataRow(true, "2025-10-01", "2017-01-01")] // 0 fees match
		public void ProcessFeeRules_ReturnsNull_WhenNoFeeFound(bool isLor, string paidDate, string renewalDueDate)
		{
			// Arrange
			Error.Add(Error.Create<FeeManagementService>("E005"));
			var ExpectedErrorMessage = $"Unable to find a single fee for the given request. A fee could not be calculated.";

			FeeCalculationRequestDetails details = new FeeCalculationRequestDetails() { IsLOR = isLor, PaidDate = paidDate, RenewalDueDate = renewalDueDate };
			List<Fee> fees = new List<Fee>
			{
				new Fee
				{
					Id = 1,
					Rule = "IF((isLOR=1)*(MIN(paidDate,renewalDueDate)>=\"06/04/2018\"),1,0)"
				},
				new Fee
				{
					Id = 2,
					Rule = "IF((isLOR=0)*(MIN(paidDate,renewalDueDate)>=\"06/04/2018\"),1,0)"
				},
				new Fee
				{
					Id = 3,
					Rule = "IF((isLOR=1)*(MIN(paidDate,renewalDueDate)>=\"06/04/2025\"),1,0)"
				}
			};

			// Act & Assert
			var resultFee = _feeCalculation.ProcessFeeRules(details, fees);

			// Assert
			resultFee.Should().BeNull();
		}

		[TestMethod]
		[DataRow(1, true, "2025-01-01", "2019-01-01")]
		[DataRow(2, false, "2025-01-01", "2019-01-01")]
		public void ProcessFeeRules_ShouldReturnCorrectFee_WhenDifferentIfDateFeeRulesExist(int expectedResult, bool isLor, string paidDate, string renewalDueDate)
		{
			// Arrange
			FeeCalculationRequestDetails details = new FeeCalculationRequestDetails() { IsLOR = isLor, PaidDate = paidDate, RenewalDueDate = renewalDueDate };
			List<Fee> fees = new List<Fee>
			{
				new Fee
				{
					Id = 1,
					Rule = "IF((isLOR=1)*(MIN(paidDate,renewalDueDate)>=\"06/04/2018\"),1,0)"
				},
				new Fee
				{
					Id = 2,
					Rule = "IF((isLOR=0)*(MIN(paidDate,renewalDueDate)>=\"06/04/2018\"),1,0)"
				}
			};

			// Act
			var resultFee = _feeCalculation.ProcessFeeRules(details, fees);

			// Assert
			resultFee.Should().NotBeNull();
			resultFee.Id.Should().Be(expectedResult);
		}

		[TestMethod]
		[DataRow(1, 20)]
		[DataRow(2, 21)]
		[DataRow(3, 22)]
		public void ProcessFeeRules_ShouldReturnCorrectFee_WhenDifferentMaxFeeRulesExist(int expectedResult, int numberOfClaims)
		{
			// Arrange
			FeeCalculationRequestDetails details = new FeeCalculationRequestDetails() { NumberOfClaims = numberOfClaims };
			List<Fee> fees = new List<Fee>
			{
				new Fee
				{
					Id = 1,
					Rule = "MAX(0,numberOfClaims <= 20)"
				},
				new Fee
				{
					Id = 2,
					Rule = "MAX(0,numberOfClaims = 21)"
				},
				new Fee
				{
					Id = 3,
					Rule = "MAX(0,numberOfClaims >= 22)"
				},
			};


			// Act
			var resultFee = _feeCalculation.ProcessFeeRules(details, fees);

			// Assert
			resultFee.Should().NotBeNull();
			resultFee.Id.Should().Be(expectedResult);
		}
		#endregion

		private async void SetupServiceRequestAndProductWithProductSequence(string channel, int productASequence, int productBSequence, int productCSequence)
		{
			string serviceRequestType = ServiceRequestTypeEnum.PatentApplicationSearchAndExam.ToString();
			string rule = Settings.NoQuantityRuleValue;
			DateTime referenceDate = DateTime.Now;

			await Helper.CreateServiceRequestTypeInRepo(_feeDbRepository, 1, serviceRequestType);
			await Helper.AddCompleteProductToServiceRequestType(_feeDbRepository, serviceRequestType, channel, ProductCodeA, "Product-A", 0.00M, "OS", 50.00M, rule, referenceDate, productASequence);
			await Helper.AddCompleteProductToServiceRequestType(_feeDbRepository, serviceRequestType, channel, ProductCodeB, "Product-B", 0.00M, "OS", 50.00M, rule, referenceDate, productBSequence);
			await Helper.AddCompleteProductToServiceRequestType(_feeDbRepository, serviceRequestType, channel, ProductCodeC, "Product-C", 0.00M, "OS", 50.00M, rule, referenceDate, productCSequence);
		}
	}
}


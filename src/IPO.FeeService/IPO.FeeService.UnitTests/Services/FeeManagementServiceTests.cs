using AutoFixture;
using AwesomeAssertions;
using IPO.Common.Infrastructure;
using IPO.FeeService.Data;
using IPO.FeeService.Interfaces;
using IPO.FeeService.Models.API;
using IPO.FeeService.Models.API.FeeInformation;
using IPO.FeeService.Models.Constants;
using IPO.FeeService.Models.Data;
using IPO.FeeService.Models.Validation;
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
using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using ServiceRequestType = IPO.FeeService.Models.Data.ServiceRequestType;
using Microsoft.Extensions.Configuration;

namespace IPO.FeeService.UnitTests.Services
{
    [TestClass]
    public class FeeManagementServiceTests
    {
        private readonly Fixture _fixture;
        private readonly IRenewalsFeeValidator _renewalsFeeValidator;
        private readonly FeeDbRepository _feeDbRepository;
        private readonly FeeManagementService _feeManagementService;
        private const string FeeManagementServiceErrorCode = "E005";
        private const string RenewalsFeeValidatorErrorCode = "E006";
        private const string ProductCodeA = "PA";
		private const string ProductCodeB = "PB";
		private const string ProductCodeC = "PC";
		private readonly Mock<ILogger<FeeDbRepository>> _mockLogger;
        private readonly Mock<IConfiguration> _feeConfiguration;

        public FeeManagementServiceTests()
        {
			_mockLogger = new Mock<ILogger<FeeDbRepository>>();
			_fixture = new Fixture();
            _feeDbRepository = new FeeDbRepository(new FeeDbContext(
                                    new DbContextOptionsBuilder<FeeDbContext>()
                                    .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                                    .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                                    .Options
                                    ),
									_mockLogger.Object);
            _renewalsFeeValidator = new RenewalsFeeValidator();
            _feeConfiguration = new Mock<IConfiguration>(MockBehavior.Strict);
            _feeConfiguration.SetupGet(x => x[It.Is<string>(s => s == "DaysToModifyDate")]).Returns("0");
            _feeManagementService = new FeeManagementService(_feeDbRepository, _renewalsFeeValidator, _feeConfiguration.Object);
            Error.Add(Error.Create<FeeManagementService>(FeeManagementServiceErrorCode));
        }

        [TestMethod]
        public async Task CalculateFeesAsyncThrowsErrorWhenServiceRequestNotFoundInDb()
        {
            // Arrange
            string serviceRequestType = "Acceleration";
            await Helper.CreateServiceRequestTypeInRepo(_feeDbRepository, 1, "Nonsense");


            FeeCalculationRequest feeCalculationRequest = new()
            {
                PaidDate = DateTime.Now.ToString("yyyy-MM-dd"),
                RequestDetails = new[] { new FeeCalculationRequestDetails() }
            };

            var expectedMessage = $"No matching service request for '{serviceRequestType}' could be found";

            // Act
            StatusCodeException ex = await Assert.ThrowsExactlyAsync<StatusCodeException>(() => _feeManagementService.CalculateFeesAsync(ServiceRequestTypeEnum.Acceleration, feeCalculationRequest, "00"));

            // Assert
            ex.Message.Should().Be(expectedMessage);
            ex.StatusCode.Should().Be(StatusCodes.Status422UnprocessableEntity);
        }

        [TestMethod]
        public void PaidDateDefaultsToCurrentDateTimeWhenNotProvided()
        {
            // Arrange
            // Act
            FeeCalculationRequest feeCalculationRequest = new()
            {
                RequestDetails = new[] { new FeeCalculationRequestDetails() }
            };

            // Assert
            feeCalculationRequest.PaidDate.Should().Be(DateTime.Now.ToString("yyyy-MM-dd"));
        }

        [TestMethod]
        [DataRow("2023-25-02")]
        [DataRow("0")]
        [DataRow("sadsadsadqwasd")]
        [DataRow("")]
        [DataRow("02-25-2023")]
        [DataRow("25-02-2023")]
        [DataRow("25-02-23")]
        [DataRow("25/02/2023")]
        [DataRow("2023/02/25")]
        [DataRow("2023.02.25")]
        public async Task CalculateFeesAsyncThrowsErrorWhenPaidDateIsInvalid(string invalidPaidDate)
        {
            // Arrange            
            Error.Add(Error.Create<FeeManagementService>(FeeManagementServiceErrorCode));
            FeeCalculationRequest feeCalculationRequest = new()
            {
                PaidDate = invalidPaidDate,
                RequestDetails = new[] { new FeeCalculationRequestDetails() }
            };

            var expectedMessage = $"The Paid Date value '{feeCalculationRequest.PaidDate}' could not be processed. A valid date must be provided and formatted as yyyy-MM-dd.";

            // Act
            StatusCodeException ex = await Assert.ThrowsExactlyAsync<StatusCodeException>(() => _feeManagementService.CalculateFeesAsync(ServiceRequestTypeEnum.Acceleration, feeCalculationRequest, "00"));

            // Assert
            ex.Message.Should().Be(expectedMessage);
            ex.StatusCode.Should().Be(StatusCodes.Status422UnprocessableEntity);
        }

        [TestMethod]
        [DataRow("05")]
        [DataRow("001")]
        [DataRow("sfdsfdsd")]
        [DataRow("-12")]
        [DataRow("01.1")]
        public async Task CalculateFeesAsyncThrowsErrorWhenChannelIsInvalid(string channel)
        {
            // Arrange            
            FeeCalculationRequest feeCalculationRequest = new()
            {
                PaidDate = DateTime.Now.ToString("yyyy-MM-dd"),
                RequestDetails = new[] { new FeeCalculationRequestDetails() }
            };

            var expectedMessage = $"The customer channel '{channel}' is not supported";

            // Act
            StatusCodeException ex = await Assert.ThrowsExactlyAsync<StatusCodeException>(() => _feeManagementService.CalculateFeesAsync(ServiceRequestTypeEnum.Acceleration, feeCalculationRequest, channel));

            // Assert
            ex.Message.Should().Be(expectedMessage);
            ex.StatusCode.Should().Be(StatusCodes.Status422UnprocessableEntity);
        }

        [TestMethod]
        [DataRow("00")]
        [DataRow("01")]
        [DataRow("10")]
        public async Task CalculateFeesAsyncHandlesMissingRequestDetails(string customerChannel)
        {
            // Arrange
            await Helper.CreateServiceRequestTypeInRepo(_feeDbRepository, 1, "TransferOfOwner");

            FeeCalculationRequest feeCalculationRequest = new()
            {
                PaidDate = DateTime.Now.ToString("yyyy-MM-dd")
            };

            // Act
            var results = await _feeManagementService.CalculateFeesAsync(ServiceRequestTypeEnum.TransferOfOwner, feeCalculationRequest, customerChannel);

            // Assert
            results.ServiceRequestType.Should().Be(ServiceRequestTypeEnum.TransferOfOwner.ToString());
            results.Should().NotBeNull();
            results.LineItems.Length.Should().Be(0);
            results.TotalIncVat.Should().Be(0);
            results.CustomerChannel.Should().Be(customerChannel);
        }

        [TestMethod]
        [DataRow("00")]
        [DataRow("01")]
        [DataRow("10")]
        public async Task CalculateFeesAsyncHandlesMissingProducts(string channel)
        {
            // Arrange
            SetupTransferOfOwnerDbValues(channel, DateTime.Now);

            FeeCalculationRequest feeCalculationRequest = new()
            {
                PaidDate = DateTime.Now.ToString("yyyy-MM-dd")
            };

            // Act
            var results = await _feeManagementService.CalculateFeesAsync(ServiceRequestTypeEnum.TransferOfOwner, feeCalculationRequest, channel);

            // Assert
            results.ServiceRequestType.Should().Be(ServiceRequestTypeEnum.TransferOfOwner.ToString());
            results.Should().NotBeNull();
            results.TotalIncVat.Should().Be(0);
            results.LineItems.Length.Should().Be(0);
            results.CustomerChannel.Should().Be(channel);
        }

        [TestMethod]
        [DataRow("2023-06-22", "2020-06-22", ServiceRequestTypeEnum.Acceleration, "00")]
        [DataRow("2023-06-22", "2024-04-22", ServiceRequestTypeEnum.Acceleration, "01")]
        [DataRow("2023-06-22", "2023-07-22", ServiceRequestTypeEnum.Acceleration, "00")]
        [DataRow("2023-06-22", "2023-07-22", ServiceRequestTypeEnum.Acceleration, "10")]
        public async Task CalculateFeesAsyncThrowsErrorWhenFeesMissing(string effectiveFromToReferenceDate, string paidDate, ServiceRequestTypeEnum serviceRequestType, string channel)
        {
            // Arrange
            await Helper.CreateServiceRequestTypeInRepo(_feeDbRepository, 1, serviceRequestType.ToString());
            var serviceRequestRecord = _feeDbRepository.Context.ServiceRequestTypes!.First(x => x.Name == serviceRequestType.ToString());

            DateTime effectiveFromToMidPoint = DateTime.ParseExact(effectiveFromToReferenceDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None);

            int feeId = _feeDbRepository.Context.Fees!.Any() ? _feeDbRepository.Context.Fees!.Last().Id + 1 : 1;
            var fee1 = Helper.CreateFee(feeId, 50.00M, effectiveFromToMidPoint.AddYears(-1), effectiveFromToMidPoint);

            int ruleId = _feeDbRepository.Context.QuantityRules!.Any() ? _feeDbRepository.Context.QuantityRules!.Last().Id + 1 : 1;
            var rule = Helper.CreateQuantityRule(ruleId, Settings.NoQuantityRuleValue, effectiveFromToMidPoint.AddYears(-5), null);

            int taxCodeId = _feeDbRepository.Context.TaxCodes!.Any() ? _feeDbRepository.Context.QuantityRules!.Last().Id + 1 : 1;
            var taxCode = Helper.CreateTaxCode(ruleId, "Os", 0.00M, effectiveFromToMidPoint.AddYears(-5), null);

            int productId = _feeDbRepository.Context.Products!.Any() ? _feeDbRepository.Context.Products!.Last().Id + 1 : 1;
            var product = Helper.CreateProduct(productId, channel, "ABC1", "Dummy product", taxCode, rule, new Collection<Fee> { fee1 });

            ProductServiceRequestType productServiceRequestType = Helper.CreateProductServiceRequestType(product, serviceRequestRecord);
            _feeDbRepository.Context.Products!.Add(product);

            foreach (var modifiedFee in product.Fees!)
            {
                _feeDbRepository.Context.Fees!.Add(modifiedFee);
            }
            await _feeDbRepository.Context.SaveChangesAsync();

            FeeCalculationRequest feeCalculationRequest = new()
            {
                PaidDate = paidDate
            };

            var expectedMessage = $"Unable to find data for the given request. A fee could not be calculated.";

            // Act
            StatusCodeException ex = await Assert.ThrowsExactlyAsync<StatusCodeException>(() => _feeManagementService.CalculateFeesAsync(serviceRequestType, feeCalculationRequest, channel));

            // Assert
            ex.Message.Should().Be(expectedMessage);
            ex.StatusCode.Should().Be(StatusCodes.Status404NotFound);
        }

        private async void SetupTransferOfOwnerDbValues(string channel, DateTime referenceDate)
        {
            string serviceRequestType = "TransferOfOwner";

            await Helper.CreateServiceRequestTypeInRepo(_feeDbRepository, 1, serviceRequestType);

            string patentsRule = "IF(numberOfPatents >= 1, 1, 0)";
            await Helper.AddCompleteProductToServiceRequestType(_feeDbRepository, serviceRequestType, channel, "P21", "Patent Transfer of Owner", 0.00M, "OS", 50.00M, patentsRule, referenceDate, 1);

            string trademarksRule = "IF(numberOfTrademarks >= 1, 1, 0)";
            await Helper.AddCompleteProductToServiceRequestType(_feeDbRepository, serviceRequestType, channel, "TM16", "Trademark Transfer of Owner", 0.00M, "OS", 50.00M, trademarksRule, referenceDate, 2);

            string designsRule = "IF(numberOfDesigns >= 1, 1, 0)";
            await Helper.AddCompleteProductToServiceRequestType(_feeDbRepository, serviceRequestType, channel, "", "Design Transfer of Owner", 0.00M, "OS", 0.00M, designsRule, referenceDate, 3);
        }

        [TestMethod]
        [DataRow("00", 1, 1, 1, "100.00", 3)]
        [DataRow("00", 2, 2, 0, "50.00", 2)]
        [DataRow("00", 0, 0, 1, "50.00", 1)]
        [DataRow("00", 0, 0, 0, "00.00", 0)]
        [DataRow("01", 1, 1, 1, "100.00", 3)]
        [DataRow("01", 2, 2, 0, "50.00", 2)]
        [DataRow("01", 0, 0, 1, "50.00", 1)]
        [DataRow("01", 0, 0, 0, "00.00", 0)]
        public async Task CalculateFeesAsyncResultsForTransferOfOwnerTest(string channel, int numberOfDesigns, int numberOfPatents, int numberOfTrademarks, string expectedTotal, int expectedLineItems)
        {
            // Arrange
            SetupTransferOfOwnerDbValues(channel, DateTime.Now);

            FeeCalculationRequest feeCalculationRequest = new()
            {
                PaidDate = DateTime.Now.ToString("yyyy-MM-dd"),
                RequestDetails = new[]
                {
            new FeeCalculationRequestDetails()
            {
                NumberOfDesigns = numberOfDesigns,
                NumberOfPatents = numberOfPatents,
                NumberOfTrademarks = numberOfTrademarks
            }
        }
            };

            // Act
            var results = await _feeManagementService.CalculateFeesAsync(ServiceRequestTypeEnum.TransferOfOwner, feeCalculationRequest, channel);

            // Assert
            results.ServiceRequestType.Should().Be(ServiceRequestTypeEnum.TransferOfOwner.ToString());
            results.Should().NotBeNull();
            results.TotalIncVat.Should().Be(decimal.Parse(expectedTotal));
            results.LineItems.Length.Should().Be(expectedLineItems);
            results.LineItems.Count(r => r.Quantity > 0).Should().Be(Helper.TransferOfOwnerQuantityCount(feeCalculationRequest));
            results.CustomerChannel.Should().Be(channel);

            if (numberOfPatents > 0)
            {
                results.LineItems.Where(x => x.E5ProductCode == "P21").Count().Should().Be(1);

                var patentsLineItem = results.LineItems.First(x => x.E5ProductCode == "P21");
                patentsLineItem.VatCode.Should().Be("OS");
                patentsLineItem.VatAmount.Should().Be(0);
                patentsLineItem.Price.Should().Be(50);
            }

            if (numberOfTrademarks > 0)
            {
                results.LineItems.Where(x => x.E5ProductCode == "TM16").Count().Should().Be(1);

                var patentsLineItem = results.LineItems.First(x => x.E5ProductCode == "TM16");
                patentsLineItem.VatCode.Should().Be("OS");
                patentsLineItem.VatAmount.Should().Be(0);
                patentsLineItem.Price.Should().Be(50);
            }

            if (numberOfDesigns > 0)
            {
                results.LineItems.Where(x => x.E5ProductCode == string.Empty).Count().Should().Be(1);

                var patentsLineItem = results.LineItems.First(x => x.E5ProductCode == string.Empty);
                patentsLineItem.VatCode.Should().Be("OS");
                patentsLineItem.VatAmount.Should().Be(0);
                patentsLineItem.Price.Should().Be(0);
            }
        }

        private async void SetupServiceRequestAndProduct(ServiceRequestTypeEnum serviceRequestType, string channel, string productStartDate, string productCode, string productName, decimal price, string rule)
        {
            await Helper.CreateServiceRequestTypeInRepo(_feeDbRepository, 1, serviceRequestType.ToString());

            DateTime referenceDate = DateTime.ParseExact(productStartDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None);

            await Helper.AddCompleteProductToServiceRequestType(_feeDbRepository, serviceRequestType.ToString(), channel, productCode, productName, 0.00M, "OS", price, rule, referenceDate);
        }

        [TestMethod]
        [DataRow("00")]
        [DataRow("01")]
        public async Task CalculateFeesAsyncResultsNoRuleQuantityTest(string channel)
        {
            // Arrange
            var serviceRequestType = ServiceRequestTypeEnum.Acceleration;
            string rule = Settings.NoQuantityRuleValue;
            string productName = "Request to accelerate application";
            SetupServiceRequestAndProduct(serviceRequestType, channel, "2023-01-01", string.Empty, productName, 0.00M, rule);

            FeeCalculationRequest feeCalculationRequest = new()
            {
                PaidDate = "2023-05-01",
                RequestDetails = new[] { new FeeCalculationRequestDetails() }
            };

            // Act
            var results = await _feeManagementService.CalculateFeesAsync(ServiceRequestTypeEnum.Acceleration, feeCalculationRequest, channel);

            // Assert
            results.Should().NotBeNull();
            results.ServiceRequestType.Should().Be(ServiceRequestTypeEnum.Acceleration.ToString());
            results.LineItems.Length.Should().Be(1);
            results.LineItems.First().ProductName.Should().Be(productName);
            results.LineItems.First().Quantity.Should().Be(1);
            results.CustomerChannel.Should().Be(channel);
        }

        [TestMethod]
        [DataRow("NumberOfCertifiedCopies", "numberOfCertifiedCopies", 0, "00")]
        [DataRow("NumberOfCertifiedCopies", "numberOfCertifiedCopies", 1, "00")]
        [DataRow("NumberOfCertifiedCopies", "numberOfCertifiedCopies", 10, "00")]
        [DataRow("NumberOfCertifiedCopies", "numberOfCertifiedCopies", 0, "01")]
        [DataRow("NumberOfCertifiedCopies", "numberOfCertifiedCopies", 1, "01")]
        [DataRow("NumberOfCertifiedCopies", "numberOfCertifiedCopies", 10, "01")]
        public async Task CalculateFeesAsyncResultsRequestPropertyQuantityRulesTest(string requestPropertyName, string rule, int requestPropertyValue, string channel)
        {
            // Arrange
            var serviceRequestType = ServiceRequestTypeEnum.Acceleration;

            string productName = "Request to accelerate application";
            decimal price = 20.00M;
            SetupServiceRequestAndProduct(serviceRequestType, channel, "2023-01-01", string.Empty, productName, price, rule);

            var requestDetails = new FeeCalculationRequestDetails();
            var propertyInfo = requestDetails.GetType().GetProperty(requestPropertyName);
            propertyInfo!.SetValue(requestDetails, requestPropertyValue, null);

            FeeCalculationRequest feeCalculationRequest = new()
            {
                PaidDate = "2023-05-01",
                RequestDetails = new[] { requestDetails }
            };

            // Act
            var results = await _feeManagementService.CalculateFeesAsync(ServiceRequestTypeEnum.Acceleration, feeCalculationRequest, channel);

            // Assert
            results.Should().NotBeNull();
            results.ServiceRequestType.Should().Be(ServiceRequestTypeEnum.Acceleration.ToString());
            results.CustomerChannel.Should().Be(channel);
            results.TotalIncVat.Should().Be(requestPropertyValue * price);
            if (requestPropertyValue > 0)
            {
                results.LineItems.Length.Should().Be(1);
                results.LineItems.First().Quantity.Should().Be(requestPropertyValue);
            }
            else
            {
                results.LineItems.Length.Should().Be(0);
            }
        }

        [ExcludeFromCodeCoverage]
        static IEnumerable<object[]> IfRulesTestData
        {
            get
            {
                return new[]
                {
                    new object[] { "IF((isSpecified = 0)*(isFoc = 0),1,0)", "P52", 30.00M, ServiceRequestTypeEnum.Reinstatement, new FeeCalculationRequestDetails { IsSpecified = false, IsFoc = false }, 1 },
                    new object[] { "IF((isSpecified = 0)*(isFoc = 0),1,0)", "P52", 30.00M, ServiceRequestTypeEnum.Reinstatement, new FeeCalculationRequestDetails { IsSpecified = true, IsFoc = false }, 0 },
                    new object[] { "IF((isSpecified = 0)*(isFoc = 0),1,0)", "P52", 30.00M, ServiceRequestTypeEnum.Reinstatement, new FeeCalculationRequestDetails { IsSpecified = false, IsFoc = true }, 0 },
                    new object[] { "IF((isSpecified = 1),1,0)", "", 25.00M, ServiceRequestTypeEnum.Reinstatement, new FeeCalculationRequestDetails { IsSpecified = true }, 1 },
                    new object[] { "IF((isSpecified = 1),1,0)", "", 25.00M, ServiceRequestTypeEnum.Reinstatement, new FeeCalculationRequestDetails { IsSpecified = false }, 0 },
                    new object[] { "IF((rightType=\"Patent\")*(patentType=\"GB\")*((fromRenewalYear+isLateGrant)<=7)*(renewalYear>=7)*(isLOR=1)*(MIN(paidDate,renewalDueDate)>=\"06/04/2018\"),1,0)", "P12-07LOR", 55.00M, ServiceRequestTypeEnum.Renewals, new FeeCalculationRequestDetails { RightId = 1, RenewalDueDate = "2023-06-01", RightType = "Patent", PatentType = "GB", FromRenewalYear = 7, RenewalYear = 7, IsLOR = true, IsLateGrant = false }, 1 },
                    new object[] { "IF((rightType=\"Patent\")*(patentType=\"GB\")*((fromRenewalYear+isLateGrant)<=7)*(renewalYear>=7)*(isLOR=1)*(MIN(paidDate,renewalDueDate)>=\"06/04/2018\"),1,0)", "P12-07LOR", 55.00M, ServiceRequestTypeEnum.Renewals, new FeeCalculationRequestDetails { RightId = 1, RenewalDueDate = "2023-06-01", RightType = "sdsadas", PatentType = "GB", FromRenewalYear = 7, RenewalYear = 7, IsLOR = true, IsLateGrant = false }, 0 },
                    new object[] { "IF((rightType=\"Patent\")*(patentType=\"GB\")*((fromRenewalYear+isLateGrant)<=7)*(renewalYear>=7)*(isLOR=1)*(MIN(paidDate,renewalDueDate)>=\"06/04/2018\"),1,0)", "P12-07LOR", 55.00M, ServiceRequestTypeEnum.Renewals, new FeeCalculationRequestDetails { RightId = 1, RenewalDueDate = "2023-06-01", RightType = "Patent", PatentType = "EP", FromRenewalYear = 7, RenewalYear = 7, IsLOR = true, IsLateGrant = false }, 0 },
                    new object[] { "IF((isLateDeclarationOfPriority = 1),1,0)" , "P3D", 150.00M, ServiceRequestTypeEnum.PatentApplication, new FeeCalculationRequestDetails { IsLateDeclarationOfPriority = true }, 1 },
                    new object[] { "IF((isLateDeclarationOfPriority = 1),1,0)" , "P3D", 150.00M, ServiceRequestTypeEnum.PatentApplication, new FeeCalculationRequestDetails { IsLateDeclarationOfPriority = false }, 0 },
                    new object[] { "IF((isPublishTranslation= 1),1,0)", "NP1-TRANS", 12.00M, ServiceRequestTypeEnum.PublicationOfTranslation, new FeeCalculationRequestDetails { IsPublishTranslation = true }, 1 },
                    new object[] { "IF((isPublishTranslation= 1),1,0)", "NP1-TRANS", 12.00M, ServiceRequestTypeEnum.PublicationOfTranslation, new FeeCalculationRequestDetails { IsPublishTranslation = false }, 0 },
                    new object[] { "IF((spcYear >= 1)*(spcYear <= 5),1,0)", "SP2-Y1", 600.00M, ServiceRequestTypeEnum.PaymentOfAnnualSpcFees, new FeeCalculationRequestDetails { SpcYear = 0}, 0 },
                    new object[] { "IF((spcYear >= 1)*(spcYear <= 5),1,0)", "SP2-Y1", 600.00M, ServiceRequestTypeEnum.PaymentOfAnnualSpcFees, new FeeCalculationRequestDetails { SpcYear = 1}, 1 },
                    new object[] { "IF((spcYear >= 2)*(spcYear <= 5),1,0)", "SP2-Y2", 700.00M, ServiceRequestTypeEnum.PaymentOfAnnualSpcFees, new FeeCalculationRequestDetails { SpcYear = 2}, 1 },
                    new object[] { "IF((spcYear >= 3)*(spcYear <= 5),1,0)", "SP2-Y3", 800.00M, ServiceRequestTypeEnum.PaymentOfAnnualSpcFees, new FeeCalculationRequestDetails { SpcYear = 3}, 1 },
                    new object[] { "IF((spcYear >= 4)*(spcYear <= 5),1,0)", "SP2-Y4", 9000.00M, ServiceRequestTypeEnum.PaymentOfAnnualSpcFees, new FeeCalculationRequestDetails { SpcYear = 4}, 1 },
                    new object[] { "IF((spcYear = 5),1,0)", "SP2-Y5", 1000.00M, ServiceRequestTypeEnum.PaymentOfAnnualSpcFees, new FeeCalculationRequestDetails { SpcYear = 5}, 1 },
                    new object[] { "IF((spcYear >= 1)*(spcYear <= 5)*(isLate = 1),1,0)", "SP2-Y1-LATE", 300.00M, ServiceRequestTypeEnum.PaymentOfAnnualSpcFees, new FeeCalculationRequestDetails { SpcYear = 0 , IsLate = false }, 0 },
                    new object[] { "IF((spcYear >= 1)*(spcYear <= 5)*(isLate = 1),1,0)", "SP2-Y1-LATE", 300.00M, ServiceRequestTypeEnum.PaymentOfAnnualSpcFees, new FeeCalculationRequestDetails { SpcYear = 1 , IsLate = false }, 0 },
                    new object[] { "IF((spcYear >= 1)*(spcYear <= 5)*(isLate = 1),1,0)", "SP2-Y1-LATE", 300.00M, ServiceRequestTypeEnum.PaymentOfAnnualSpcFees, new FeeCalculationRequestDetails { SpcYear = 1 , IsLate = true }, 1 },
                    new object[] { "IF((spcYear >= 2)*(spcYear <= 5)*(isLate = 1),1,0)", "SP2-Y2-LATE", 350.00M, ServiceRequestTypeEnum.PaymentOfAnnualSpcFees, new FeeCalculationRequestDetails { SpcYear = 2 , IsLate = true }, 1 },
                    new object[] { "IF((spcYear >= 3)*(spcYear <= 5)*(isLate = 1),1,0)", "SP2-Y3-LATE", 400.00M, ServiceRequestTypeEnum.PaymentOfAnnualSpcFees, new FeeCalculationRequestDetails { SpcYear = 3 , IsLate = true }, 1 },
                    new object[] { "IF((spcYear >= 4)*(spcYear <= 5)*(isLate = 1),1,0)", "SP2-Y4-LATE", 450.00M, ServiceRequestTypeEnum.PaymentOfAnnualSpcFees, new FeeCalculationRequestDetails { SpcYear = 4 , IsLate = true }, 1 },
                    new object[] { "IF((spcYear = 5)*(isLate = 1),1,0)", "SP2-Y5-LATE", 500.00M, ServiceRequestTypeEnum.PaymentOfAnnualSpcFees, new FeeCalculationRequestDetails { SpcYear = 5 , IsLate = true }, 1 },
                    new object[] { "IF(numberOfPages<35, MAX(numberOfPages + numberOfPagesCorrection - 35, 0), MAX(35 - numberOfPages, numberOfPagesCorrection))", "P10EDCR", 10.00M, ServiceRequestTypeEnum.PatentApplicationSearchAndExam, new FeeCalculationRequestDetails { NumberOfPages = 40 ,NumberOfPagesCorrection = 5 }, 5 },
                    new object[] { "IF(numberOfClaims<25, MAX(numberOfClaims + numberOfClaimsCorrection - 25, 0), MAX(25 - numberOfClaims, numberOfClaimsCorrection))", "P9AECCR", 20.00M, ServiceRequestTypeEnum.PatentApplicationSearchAndExam, new FeeCalculationRequestDetails { NumberOfClaims = 30, NumberOfClaimsCorrection = 10 }, 10 },
                    new object[] { "IF(numberOfPages<35, MAX(numberOfPages + numberOfPagesCorrection - 35, 0), MAX(35 - numberOfPages, numberOfPagesCorrection))", "P10EDCR", 10.00M, ServiceRequestTypeEnum.PatentApplicationSearchAndExam, new FeeCalculationRequestDetails { NumberOfPages = 47 ,NumberOfPagesCorrection = -5 }, -5 },
                    new object[] { "IF(numberOfClaims<25, MAX(numberOfClaims + numberOfClaimsCorrection - 25, 0), MAX(25 - numberOfClaims, numberOfClaimsCorrection))", "P9AECCR", 20.00M, ServiceRequestTypeEnum.PatentApplicationSearchAndExam, new FeeCalculationRequestDetails { NumberOfClaims = 35, NumberOfClaimsCorrection = -10 }, -10 },
                    new object[] { "IF(numberOfPages<35, MAX(numberOfPages + numberOfPagesCorrection - 35, 0), MAX(35 - numberOfPages, numberOfPagesCorrection))", "P10EDCR", 10.00M, ServiceRequestTypeEnum.PatentApplicationSearchAndExam, new FeeCalculationRequestDetails { NumberOfPages = 35 ,NumberOfPagesCorrection = 0 }, 0 },
                    new object[] { "IF((rightType=\"Patent\")*(patentType=\"GB\")*(fromRenewalYear<=5)*(renewalYear>=5)*(isLOR=0)*(MIN(paidDate,renewalDueDate)>=\"06/04/2018\"),1,0)", "P12-05", 70.00M, ServiceRequestTypeEnum.Renewals, new FeeCalculationRequestDetails { RightId = 1, RenewalDueDate = "2023-06-01", RightType = "Patent", PatentType = "GB", FromRenewalYear = 5, RenewalYear = 5, IsLOR = false, IsLateGrant = false }, 1 },
                    new object[] { "IF((rightType=\"Patent\")*(patentType=\"GB\")*(isRestoration=0)*(#MonthsLate>=1)*(MIN(paidDate,renewalDueDate)>=\"06/04/2018\"),#MonthsLate-1,0)", "P12-05", 70.00M, ServiceRequestTypeEnum.Renewals, new FeeCalculationRequestDetails { RightId = 1, RenewalDueDate = "2023-01-01", RightType = "Patent", PatentType = "GB", FromRenewalYear = 5, RenewalYear = 5, IsLOR = false, IsLateGrant = false, IsRestoration = false }, 2 },
					new object[] { "IF((rightType=\"Patent\")*(patentType=\"GB\")*(isRestoration=0)*(#MonthsLate>=1)*(MIN(paidDate,renewalDueDate)>=\"06/04/2018\"),#MonthsLate-1,0)", "P12-05", 70.00M, ServiceRequestTypeEnum.Renewals, new FeeCalculationRequestDetails { RightId = 1, RenewalDueDate = "2023-01-01", RightType = "Patent", PatentType = "GB", FromRenewalYear = 5, RenewalYear = 5, IsLOR = false, IsLateGrant = false, IsRestoration = true }, 0 }
				};
            }
        }

        [TestMethod]
        [DynamicData(nameof(IfRulesTestData))]
        public async Task CalculateFeesAsyncResultsIfRulesTest_Channel00(string rule, string productCode, decimal price, ServiceRequestTypeEnum serviceRequestType, FeeCalculationRequestDetails requestDetails, int expectedQuantity)
        {
            await RunCalculateFeesAsyncResultsIfRulesTest("00", rule, productCode, price, serviceRequestType, requestDetails, expectedQuantity);
        }

        [TestMethod]
        [DynamicData(nameof(IfRulesTestData))]
        public async Task CalculateFeesAsyncResultsIfRulesTest_Channel01(string rule, string productCode, decimal price, ServiceRequestTypeEnum serviceRequestType, FeeCalculationRequestDetails requestDetails, int expectedQuantity)
        {
            await RunCalculateFeesAsyncResultsIfRulesTest("01", rule, productCode, price, serviceRequestType, requestDetails, expectedQuantity);
        }

        private async Task RunCalculateFeesAsyncResultsIfRulesTest(string channel, string rule, string productCode, decimal price, ServiceRequestTypeEnum serviceRequestType, FeeCalculationRequestDetails requestDetails, int expectedQuantity)
        {
            // Arrange
            SetupServiceRequestAndProduct(serviceRequestType, channel, "2023-01-01", productCode, string.Empty, price, rule);

            FeeCalculationRequest feeCalculationRequest = new()
            {
                PaidDate = "2023-05-01",
                RequestDetails = new[] { requestDetails }
            };

            // Act
            var results = await _feeManagementService.CalculateFeesAsync(serviceRequestType, feeCalculationRequest, channel);

            // Assert
            results.Should().NotBeNull();
            results.ServiceRequestType.Should().Be(serviceRequestType.ToString());
            results.CustomerChannel.Should().Be(channel);
            results.TotalIncVat.Should().Be(expectedQuantity * price);
            if (expectedQuantity != 0)
            {
                results.LineItems.Length.Should().Be(1);
                results.LineItems.First().Quantity.Should().Be(expectedQuantity);
            }
            else
            {
                results.LineItems.Length.Should().Be(0);
            }
        }

        [ExcludeFromCodeCoverage]
        static IEnumerable<object[]> MaxRulesTestData
        {
            get
            {
                return new[]
                {
                    new object[] { "MAX(0,numberOfPages - 35)", "P34ED", 10.00M, ServiceRequestTypeEnum.Reinstatement, new FeeCalculationRequestDetails { NumberOfPages = 0 }, 0 },
                    new object[] { "MAX(0,numberOfPages - 35)", "P34ED", 10.00M, ServiceRequestTypeEnum.Reinstatement, new FeeCalculationRequestDetails { NumberOfPages = 35 }, 0 },
                    new object[] { "MAX(0,numberOfPages - 35)", "P34ED", 10.00M, ServiceRequestTypeEnum.Reinstatement, new FeeCalculationRequestDetails { NumberOfPages = 36 }, 1 },
                    new object[] { "MAX(0,numberOfPages - 35)", "P34ED", 10.00M, ServiceRequestTypeEnum.Reinstatement, new FeeCalculationRequestDetails { NumberOfPages = 40 }, 5 },
                };
            }
        }

        [TestMethod]
        [DynamicData(nameof(MaxRulesTestData))]
        public async Task CalculateFeesAsyncResultsMaxRulesTest(string rule, string productCode, decimal price, ServiceRequestTypeEnum serviceRequestType, FeeCalculationRequestDetails requestDetails, int expectedQuantity)
        {
            // Arrange
            string channel = "00";
            SetupServiceRequestAndProduct(serviceRequestType, channel, "2023-01-01", productCode, string.Empty, price, rule);

            FeeCalculationRequest feeCalculationRequest = new()
            {
                PaidDate = "2023-05-01",
                RequestDetails = new[] { requestDetails }
            };

            // Act
            var results = await _feeManagementService.CalculateFeesAsync(serviceRequestType, feeCalculationRequest, channel);

            // Assert
            results.Should().NotBeNull();
            results.ServiceRequestType.Should().Be(serviceRequestType.ToString());
            results.CustomerChannel.Should().Be(channel);
            results.TotalIncVat.Should().Be(expectedQuantity * price);
            if (expectedQuantity > 0)
            {
                results.LineItems.Length.Should().Be(1);
                results.LineItems.First().Quantity.Should().Be(expectedQuantity);
            }
            else
            {
                results.LineItems.Length.Should().Be(0);
            }
        }

        [TestMethod]
        [DataRow("2023-01-01", "2023-05-15", "2023-01-15", 0, "00")] //General - Channel 00
        [DataRow("2023-01-01", "2023-03-15", "2023-05-15", 1, "00")] //General - Channel 00
        [DataRow("2023-01-01", "2023-03-15", "2023-06-15", 2, "00")] //General - Channel 00
        [DataRow("2023-01-01", "2023-03-15", "2023-07-15", 3, "00")] //General - Channel 00
        [DataRow("2023-01-01", "2023-02-28", "2023-04-28", 1, "00")] //Working day - Channel 00
        [DataRow("2023-01-01", "2023-02-28", "2023-04-30", 1, "00")] //Working day - Channel 00
        [DataRow("2023-01-01", "2023-02-28", "2023-05-02", 1, "00")] //Working day - Channel 00
        [DataRow("2023-01-01", "2023-02-28", "2023-05-03", 2, "00")] //Working day - Channel 00
        [DataRow("2023-01-01", "2023-09-28", "2024-01-02", 2, "00")] //Transition between years - Channel 00
        [DataRow("2023-01-01", "2023-09-28", "2024-01-30", 3, "00")] //Transition between years - Channel 00
        [DataRow("2023-01-01", "2023-09-28", "2024-02-20", 4, "00")] //Transition between years - Channel 00
        [DataRow("2023-01-01", "2023-05-15", "2023-01-15", 0, "01")] //General - Channel 01
        [DataRow("2023-01-01", "2023-03-15", "2023-05-15", 1, "01")] //General - Channel 01
        [DataRow("2023-01-01", "2023-03-15", "2023-06-15", 2, "01")] //General - Channel 01
        [DataRow("2023-01-01", "2023-03-15", "2023-07-15", 3, "01")] //General - Channel 01
        [DataRow("2023-01-01", "2023-02-28", "2023-04-28", 1, "01")] //Working day - Channel 01
        [DataRow("2023-01-01", "2023-02-28", "2023-04-30", 1, "01")] //Working day - Channel 01
        [DataRow("2023-01-01", "2023-02-28", "2023-05-02", 1, "01")] //Working day - Channel 01
        [DataRow("2023-01-01", "2023-02-28", "2023-05-03", 2, "01")] //Working day - Channel 01
        [DataRow("2023-01-01", "2023-09-28", "2024-01-02", 2, "01")] //Transition between years - Channel 01
        [DataRow("2023-01-01", "2023-09-28", "2024-01-30", 3, "01")] //Transition between years - Channel 01
        [DataRow("2023-01-01", "2023-09-28", "2024-02-20", 4, "01")] //Transition between years - Channel 01
        [DataRow("2023-01-01", "2023-05-15", "2023-01-15", 0, "10")] //General - Channel 10
        [DataRow("2023-01-01", "2023-03-15", "2023-05-15", 1, "10")] //General - Channel 10
        [DataRow("2023-01-01", "2023-03-15", "2023-06-15", 2, "10")] //General - Channel 10
        [DataRow("2023-01-01", "2023-03-15", "2023-07-15", 3, "10")] //General - Channel 10
        [DataRow("2023-01-01", "2023-02-28", "2023-04-28", 1, "10")] //Working day - Channel 10
        [DataRow("2023-01-01", "2023-02-28", "2023-04-30", 1, "10")] //Working day - Channel 10
        [DataRow("2023-01-01", "2023-02-28", "2023-05-02", 1, "10")] //Working day - Channel 10
        [DataRow("2023-01-01", "2023-02-28", "2023-05-03", 2, "10")] //Working day - Channel 10
        [DataRow("2023-01-01", "2023-09-28", "2024-01-02", 2, "10")] //Transition between years - Channel 10
        [DataRow("2023-01-01", "2023-09-28", "2024-01-30", 3, "10")] //Transition between years - Channel 10
        [DataRow("2023-01-01", "2023-09-28", "2024-02-20", 4, "10")] //Transition between years - Channel 10
        public async Task CalculateFeesAsyncRenewalsMonthsLateTestWithNoRestoration(string productStartDate, string renewalDueDate, string paidDate, int expectedQuantity, string channel)
        {
            // Arrange
            var serviceRequestType = ServiceRequestTypeEnum.Renewals;
            string productCode = "P12-LATE";
            string productName = "Patent Renewal-Late Fee";
            var price = 24.00M;
            string rule = "IF((rightType=\"Patent\")*(patentType=\"GB\")*(isRestoration=0)*(#MonthsLate>=1)*(MIN(paidDate,renewalDueDate)>=\"06/04/2018\"),#MonthsLate-1,0)";

            SetupServiceRequestAndProduct(serviceRequestType, channel, productStartDate, productCode, productName, price, rule);

            FeeCalculationRequest feeCalculationRequest = new()
            {
                PaidDate = paidDate,
                RequestDetails = new[]
                {
                    new FeeCalculationRequestDetails()
                    {
                        RenewalDueDate = renewalDueDate,
                        RightType = "Patent",
                        PatentType = "GB",
                        RightId = 1,
                        FromRenewalYear = 0,
                        RenewalYear = 0,
                        IsLateGrant = false,
                        IsLOR = false,
                        IsRestoration = false
                    }
                }
            };

            // Act
            var results = await _feeManagementService.CalculateFeesAsync(serviceRequestType, feeCalculationRequest, channel);

            // Assert
            results.Should().NotBeNull();
            results.ServiceRequestType.Should().Be(serviceRequestType.ToString());
            results.CustomerChannel.Should().Be(channel);
            results.TotalIncVat.Should().Be(expectedQuantity * price);
            if (expectedQuantity > 0)
            {
                results.LineItems.Length.Should().Be(1);
                results.LineItems.First().Quantity.Should().Be(expectedQuantity);
            }
            else
            {
                results.LineItems.Length.Should().Be(0);
            }
        }

        [TestMethod]
        [DataRow("2023-01-01", "2023-05-15", "2023-01-15", 0, "00")] //General - Channel 00
        [DataRow("2023-01-01", "2023-03-15", "2023-05-15", 0, "00")] //General - Channel 00
        [DataRow("2023-01-01", "2023-03-15", "2023-06-15", 0, "00")] //General - Channel 00
        [DataRow("2023-01-01", "2023-03-15", "2023-07-15", 0, "00")] //General - Channel 00
        [DataRow("2023-01-01", "2023-02-28", "2023-04-28", 0, "00")] //Working day - Channel 00
        [DataRow("2023-01-01", "2023-02-28", "2023-04-30", 0, "00")] //Working day - Channel 00
        [DataRow("2023-01-01", "2023-02-28", "2023-05-02", 0, "00")] //Working day - Channel 00
        [DataRow("2023-01-01", "2023-02-28", "2023-05-03", 0, "00")] //Working day - Channel 00
        [DataRow("2023-01-01", "2023-09-28", "2024-01-02", 0, "00")] //Transition between years - Channel 00
        [DataRow("2023-01-01", "2023-09-28", "2024-01-30", 0, "00")] //Transition between years - Channel 00
        [DataRow("2023-01-01", "2023-09-28", "2024-02-20", 0, "00")] //Transition between years - Channel 00
        [DataRow("2023-01-01", "2023-05-15", "2023-01-15", 0, "01")] //General - Channel 01
        [DataRow("2023-01-01", "2023-03-15", "2023-05-15", 0, "01")] //General - Channel 01
        [DataRow("2023-01-01", "2023-03-15", "2023-06-15", 0, "01")] //General - Channel 01
        [DataRow("2023-01-01", "2023-03-15", "2023-07-15", 0, "01")] //General - Channel 01
        [DataRow("2023-01-01", "2023-02-28", "2023-04-28", 0, "01")] //Working day - Channel 01
        [DataRow("2023-01-01", "2023-02-28", "2023-04-30", 0, "01")] //Working day - Channel 01
        [DataRow("2023-01-01", "2023-02-28", "2023-05-02", 0, "01")] //Working day - Channel 01
        [DataRow("2023-01-01", "2023-02-28", "2023-05-03", 0, "01")] //Working day - Channel 01
        [DataRow("2023-01-01", "2023-09-28", "2024-01-02", 0, "01")] //Transition between years - Channel 01
        [DataRow("2023-01-01", "2023-09-28", "2024-01-30", 0, "01")] //Transition between years - Channel 01
        [DataRow("2023-01-01", "2023-09-28", "2024-02-20", 0, "01")] //Transition between years - Channel 01
        public async Task CalculateFeesAsyncRenewalsMonthsLateTestWithRestoration(string productStartDate, string renewalDueDate, string paidDate, int expectedQuantity, string channel)
        {
            // Arrange
            var serviceRequestType = ServiceRequestTypeEnum.Renewals;
            string productCode = "P12-LATE";
            string productName = "Patent Renewal-Late Fee";
            var price = 24.00M;
            string rule = "IF((rightType=\"Patent\")*(patentType=\"GB\")*(isRestoration=0)*(#MonthsLate>=1)*(MIN(paidDate,renewalDueDate)>=\"06/04/2018\"),#MonthsLate-1,0)";

            SetupServiceRequestAndProduct(serviceRequestType, channel, productStartDate, productCode, productName, price, rule);

            FeeCalculationRequest feeCalculationRequest = new()
            {
                PaidDate = paidDate,
                RequestDetails = new[]
                {
                    new FeeCalculationRequestDetails()
                    {
                        RenewalDueDate = renewalDueDate,
                        RightType = "Patent",
                        PatentType = "GB",
                        RightId = 1,
                        FromRenewalYear = 0,
                        RenewalYear = 0,
                        IsLateGrant = false,
                        IsLOR = false,
                        IsRestoration = true
                    }
                }
            };

            // Act
            var results = await _feeManagementService.CalculateFeesAsync(serviceRequestType, feeCalculationRequest, channel);

            // Assert
            results.Should().NotBeNull();
            results.ServiceRequestType.Should().Be(serviceRequestType.ToString());
            results.CustomerChannel.Should().Be(channel);
            results.TotalIncVat.Should().Be(expectedQuantity * price);
            results.LineItems.Length.Should().Be(0);
        }

        [TestMethod]
        [DataRow("00")]
        [DataRow("01")]
        [DataRow("10")]
        public async Task CalculateFeesAsyncThrowsErrorWhenRuleIsBlank(string channel)
        {
            // Arrange            
            Error.Add(Error.Create<FeeManagementService>(FeeManagementServiceErrorCode));
            FeeCalculationRequest feeCalculationRequest = new()
            {
                PaidDate = "2023-05-01",
                RequestDetails = new[] { new FeeCalculationRequestDetails() }
            };
            string rule = string.Empty;
            SetupServiceRequestAndProduct(ServiceRequestTypeEnum.Acceleration, channel, "2023-01-01", "N/A", string.Empty, 0.00M, rule);

            var expectedMessage = $"The quantity rule for this product is blank.";

            // Act
            StatusCodeException ex = await Assert.ThrowsExactlyAsync<StatusCodeException>(() => _feeManagementService.CalculateFeesAsync(ServiceRequestTypeEnum.Acceleration, feeCalculationRequest, channel));

            // Assert
            ex.Message.Should().Be(expectedMessage);
            ex.StatusCode.Should().Be(StatusCodes.Status406NotAcceptable);
        }

        [TestMethod]
        [DataRow("00")]
        [DataRow("01")]
        [DataRow("10")]
        public async Task CalculateFeesAsyncThrowsErrorWhenStringPropertyInRuleNotProvided(string channel)
        {
            // Arrange            
            Error.Add(Error.Create<FeeManagementService>(FeeManagementServiceErrorCode));
            FeeCalculationRequest feeCalculationRequest = new()
            {
                PaidDate = "2023-05-01",
                RequestDetails = new[] { new FeeCalculationRequestDetails() }
            };
            string rule = "rightType";
            SetupServiceRequestAndProduct(ServiceRequestTypeEnum.Acceleration, channel, "2023-01-01", "N/A", string.Empty, 0.00M, rule);

            var expectedMessage = $"Unable to find match between rule and request details property: {rule}";

            // Act
            StatusCodeException ex = await Assert.ThrowsExactlyAsync<StatusCodeException>(() => _feeManagementService.CalculateFeesAsync(ServiceRequestTypeEnum.Acceleration, feeCalculationRequest, channel));

            // Assert
            ex.Message.Should().Be(expectedMessage);
            ex.StatusCode.Should().Be(StatusCodes.Status406NotAcceptable);
        }

        [TestMethod]
        [DataRow("IF((isSpecified = 1),1,0,1)")]
        [DataRow("IF((isSpecified = 1),1)")]
        [DataRow("MAX(1,1,1)")]
        [DataRow("MAX(1)")]
        public async Task CalculateFeesAsyncThrowsErrorWhenRuleHasWrongNumberOfArgs(string rule)
        {
            // Arrange            
            Error.Add(Error.Create<FeeManagementService>(FeeManagementServiceErrorCode));
            FeeCalculationRequest feeCalculationRequest = new()
            {
                PaidDate = "2023-05-01",
                RequestDetails = new[] { new FeeCalculationRequestDetails() }
            };

            SetupServiceRequestAndProduct(ServiceRequestTypeEnum.Acceleration, "00", "2023-01-01", "N/A", string.Empty, 0.00M, rule);

            var expectedMessage = $"Wrong number of arguments in quantity rule: {rule}.";

            // Act
            StatusCodeException ex = await Assert.ThrowsExactlyAsync<StatusCodeException>(() => _feeManagementService.CalculateFeesAsync(ServiceRequestTypeEnum.Acceleration, feeCalculationRequest, "00"));

            // Assert
            ex.Message.Should().Be(expectedMessage);
            ex.StatusCode.Should().Be(StatusCodes.Status406NotAcceptable);
        }

        #region Renewals
        [TestMethod]
        [DataRow("00")]
        [DataRow("01")]
        [DataRow("10")]
        public async Task CalculateFeesAsyncHandlesRenewalRightIdsByQuantityBreakdown(string channel)
        {
            // Arrange            
            Error.Add(Error.Create<FeeManagementService>(FeeManagementServiceErrorCode));

            var testData = new List<(int?, string)>
            {
                (1234567891, "EP"),
                (1234567892, "EP"),
                (1234567893, "EP"),
                (1234567894, "EP"),
                (1234567895, "EP"),
                (1234567896, "GB"),
                (1234567897, "GB"),
            };

            var requestDetails = new List<FeeCalculationRequestDetails>();
            foreach (var value in testData)
            {
                requestDetails.Add(new()
                {
                    RightId = value.Item1,
                    RightType = "Patent",
                    PatentType = value.Item2,
                    FromRenewalYear = 5,
                    RenewalYear = 5,
                    IsLateGrant = false,
                    IsLOR = false,
                    RenewalDueDate = "2023-04-16"
                });
            }

            FeeCalculationRequest feeCalculationRequest = new()
            {
                PaidDate = "2023-07-13",
                RequestDetails = requestDetails.ToArray()
            };

            var serviceRequestType = ServiceRequestTypeEnum.Renewals;
            var referenceDate = DateTime.ParseExact("2023-01-01", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None);

            await Helper.CreateServiceRequestTypeInRepo(_feeDbRepository, 1, serviceRequestType.ToString());

            string gbProductCode = "P12-05";
            string gbRule = "IF((rightType=\"Patent\")*(patentType=\"GB\")*(fromRenewalYear<=5)*(renewalYear>=5)*(isLOR=0)*(MIN(paidDate,renewalDueDate)>=\"06/04/2018\"),1,0)";
            var gbPrice = 60.00M;
            await Helper.AddCompleteProductToServiceRequestType(_feeDbRepository, serviceRequestType.ToString(), channel, gbProductCode, "Patent Renewal-Year05", 0.00M, "OS", gbPrice, gbRule, referenceDate, 1);

            string epProductCode = "EP12-05";
            string epRule = "IF((rightType=\"Patent\")*(patentType=\"EP\")*(fromRenewalYear<=5)*(renewalYear>=5)*(isLOR=0)*(MIN(paidDate,renewalDueDate)>=\"06/04/2018\"),1,0)";
            var epPrice = 70.00M;
            await Helper.AddCompleteProductToServiceRequestType(_feeDbRepository, serviceRequestType.ToString(), channel, epProductCode, "EPatent Renewal-Year05", 0.00M, "OS", epPrice, epRule, referenceDate, 2);

            string gbLateProductCode = "P12-LATE5";
            string gbLateRule = "IF((rightType=\"Patent\")*(patentType=\"GB\")*(isRestoration=0)*(#MonthsLate>=1)*(MIN(paidDate,renewalDueDate)>=\"06/04/2018\"),#MonthsLate-1,0)";
            var gbLatePrice = 24.00M;
            await Helper.AddCompleteProductToServiceRequestType(_feeDbRepository, serviceRequestType.ToString(), channel, gbLateProductCode, "Patent Renewal-Late Fee", 0.00M, "OS", gbLatePrice, gbLateRule, referenceDate, 3);

            // Act
            var results = await _feeManagementService.CalculateFeesAsync(serviceRequestType, feeCalculationRequest, channel);

            // Assert
            results.Should().NotBeNull();
            results.ServiceRequestType.Should().Be(serviceRequestType.ToString());
            results.CustomerChannel.Should().Be(channel);
            results.TotalIncVat.Should().Be((2 * gbPrice) + (5 * epPrice) + (4 * gbLatePrice));
            results.LineItems.Length.Should().Be(3);

            var gbLineItem = results.LineItems.First(x => x.E5ProductCode == gbProductCode);
            gbLineItem.Quantity.Should().Be(2);
            gbLineItem.ProductDetails.Should().NotBeNull();
            var gbLineItemRightsBreakDown = gbLineItem.ProductDetails as RightIdsByQuantityBreakdown;
            gbLineItemRightsBreakDown!.RightIdsByQuantity![0].Quantity.Should().Be(1);
            gbLineItemRightsBreakDown.RightIdsByQuantity[0].RightIds.Should().Contain(1234567896);
            gbLineItemRightsBreakDown.RightIdsByQuantity[0].RightIds.Should().Contain(1234567897);

            var gbLateLineItem = results.LineItems.First(x => x.E5ProductCode == gbLateProductCode);
            gbLateLineItem.Quantity.Should().Be(4);
            gbLateLineItem.ProductDetails.Should().NotBeNull();
            gbLateLineItem.ProductDetails.Should().BeAssignableTo<RightIdsByQuantityBreakdown>();
            var gbLateLineItemRightsBreakDown = gbLateLineItem.ProductDetails as RightIdsByQuantityBreakdown;
            gbLateLineItemRightsBreakDown!.RightIdsByQuantity![0].Quantity.Should().Be(2);
            gbLateLineItemRightsBreakDown.RightIdsByQuantity[0].RightIds.Should().Contain(1234567896);
            gbLateLineItemRightsBreakDown.RightIdsByQuantity[0].RightIds.Should().Contain(1234567897);

            var epLineItem = results.LineItems.First(x => x.E5ProductCode == epProductCode);
            epLineItem.Quantity.Should().Be(5);
            epLineItem.ProductDetails.Should().NotBeNull();
            var epLineItemRightsBreakDown = epLineItem.ProductDetails as RightIdsByQuantityBreakdown;
            epLineItemRightsBreakDown!.RightIdsByQuantity![0].Quantity.Should().Be(1);
            epLineItemRightsBreakDown.RightIdsByQuantity[0].RightIds.Should().Contain(1234567891);
            epLineItemRightsBreakDown.RightIdsByQuantity[0].RightIds.Should().Contain(1234567892);
            epLineItemRightsBreakDown.RightIdsByQuantity[0].RightIds.Should().Contain(1234567893);
            epLineItemRightsBreakDown.RightIdsByQuantity[0].RightIds.Should().Contain(1234567894);
            epLineItemRightsBreakDown.RightIdsByQuantity[0].RightIds.Should().Contain(1234567895);
        }

        [TestMethod]
        [DataRow("2023-25-02", "00")]
        [DataRow("2023-25-02", "01")]
        [DataRow("2023-25-02", "10")]
        [DataRow("0", "00")]
        [DataRow("0", "01")]
        [DataRow("0", "10")]
        [DataRow("sadsadsadqwasd", "00")]
        [DataRow("sadsadsadqwasd", "01")]
        [DataRow("sadsadsadqwasd", "10")]
        [DataRow("", "00")]
        [DataRow("", "01")]
        [DataRow("", "10")]
        [DataRow("02-25-2023", "00")]
        [DataRow("02-25-2023", "01")]
        [DataRow("02-25-2023", "10")]
        [DataRow("25-02-2023", "00")]
        [DataRow("25-02-2023", "01")]
        [DataRow("25-02-2023", "10")]
        [DataRow("25-02-23", "00")]
        [DataRow("25-02-23", "01")]
        [DataRow("25-02-23", "10")]
        [DataRow("25/02/2023", "00")]
        [DataRow("25/02/2023", "01")]
        [DataRow("25/02/2023", "10")]
        [DataRow("2023/02/25", "00")]
        [DataRow("2023/02/25", "01")]
        [DataRow("2023/02/25", "10")]
        [DataRow("2023.02.25", "00")]
        [DataRow("2023.02.25", "01")]
        [DataRow("2023.02.25", "10")]
        public async Task CalculateFeesAsyncThrowsErrorWhenRenewalDueDateIsInvalid(string invalidRenewalDueDate, string channel)
        {
            // Arrange            
            Error.Add(Error.Create<RenewalsFeeValidator>(RenewalsFeeValidatorErrorCode));
            FeeCalculationRequest feeCalculationRequest = new()
            {
                PaidDate = DateTime.Now.ToString("yyyy-MM-dd"),
                RequestDetails = new[]
                {
                    new FeeCalculationRequestDetails()
                    {
                        RenewalDueDate = invalidRenewalDueDate,
                        RightType = "Patent",
                        PatentType = "GB",
                        RightId = 1,
                        FromRenewalYear = 0,
                        RenewalYear = 0,
                        IsLateGrant = false,
                        IsLOR = false
                    }
                }
            };

            SetupServiceRequestAndProduct(ServiceRequestTypeEnum.Renewals, channel, "2023-01-01", string.Empty, string.Empty, 0.00M, string.Empty);

            var expectedMessage = $"The RenewalsFeeValidator encountered an error. The Renewal Due Date value '{invalidRenewalDueDate}' could not be processed. A valid date must be provided and formatted as yyyy-MM-dd.";

            // Act
            StatusCodeExceptionList exl = await Assert.ThrowsExactlyAsync<StatusCodeExceptionList>(() => _feeManagementService.CalculateFeesAsync(ServiceRequestTypeEnum.Renewals, feeCalculationRequest, channel));

            // Assert
            exl.StatusCode.Should().Be(StatusCodes.Status422UnprocessableEntity);
            exl.Errors.Should().NotBeEmpty();
            exl.Errors.Count.Should().Be(1);
            exl.Errors.FirstOrDefault()!.Code.Should().Be(RenewalsFeeValidatorErrorCode);
            exl.Errors.FirstOrDefault()!.Description.Should().Be(expectedMessage);
        }

        [TestMethod]
        [DataRow("00")]
        [DataRow("01")]
        [DataRow("10")]
        public async Task CalculateFeesAsyncThrowsMultipleErrorsWhenRenewalFieldsAreMissing(string channel)
        {
            // Arrange            
            Error.Add(Error.Create<RenewalsFeeValidator>(RenewalsFeeValidatorErrorCode));
            FeeCalculationRequest feeCalculationRequest = new()
            {
                PaidDate = DateTime.Now.ToString("yyyy-MM-dd"),
                RequestDetails = new[] { new FeeCalculationRequestDetails() }
            };

            SetupServiceRequestAndProduct(ServiceRequestTypeEnum.Renewals, channel, "2023-01-01", string.Empty, string.Empty, 0.00M, string.Empty);

            var expectedMessage = $"Exception of type 'IPO.Common.Infrastructure.StatusCodeExceptionList' was thrown.";

            // Act
            StatusCodeExceptionList exl = await Assert.ThrowsExactlyAsync<StatusCodeExceptionList>(() => _feeManagementService.CalculateFeesAsync(ServiceRequestTypeEnum.Renewals, feeCalculationRequest, channel));

            // Assert
            exl.Message.Should().Be(expectedMessage);
            exl.StatusCode.Should().Be(StatusCodes.Status422UnprocessableEntity);
            exl.Errors.Should().NotBeEmpty();
            exl.Errors.Count.Should().Be(8);
        }

        [TestMethod]
        [DataRow(null, null, null, null, null, null, null, "00")]
        [DataRow(null, "", "", null, null, null, null, "00")]
        [DataRow(null, null, null, null, null, null, null, "01")]
        [DataRow(null, "", "", null, null, null, null, "01")]
        [DataRow(null, null, null, null, null, null, null, "10")]
        [DataRow(null, "", "", null, null, null, null, "10")]
        public async Task CalculateFeesAsyncThrowsAListOfErrorsWhenRenewalFieldsAreMissing(int? rightId, string rightType, string patentType, int? fromRenewalYear, int? renewalYear, bool? isLateGrant, bool? isLor, string channel)
        {
            //Arrange
            Error.Add(Error.Create<RenewalsFeeValidator>(RenewalsFeeValidatorErrorCode));
            FeeCalculationRequest feeCalculationRequest = new()
            {
                PaidDate = DateTime.Now.ToString("yyyy-MM-dd"),
                RequestDetails = new[] { new FeeCalculationRequestDetails() {
                        RightId = rightId,
                        RightType = rightType,
                        PatentType = patentType,
                        FromRenewalYear = fromRenewalYear,
                        RenewalYear = renewalYear,
                        IsLateGrant = isLateGrant,
                        IsLOR = isLor
                    }
                }
            };

            SetupServiceRequestAndProduct(ServiceRequestTypeEnum.Renewals, channel, "2023-01-01", string.Empty, string.Empty, 0.00M, string.Empty);

            //Act
            StatusCodeExceptionList exl = await Assert.ThrowsExactlyAsync<StatusCodeExceptionList>(() => _feeManagementService.CalculateFeesAsync(ServiceRequestTypeEnum.Renewals, feeCalculationRequest, channel));

            //Assert
            exl.StatusCode.Should().Be(StatusCodes.Status422UnprocessableEntity);
            exl.Errors.Should().NotBeEmpty();
            exl.Errors.Count.Should().Be(8);
        }

        [TestMethod]
        [DataRow("rightId", "00")]
        [DataRow("rightType", "00")]
        [DataRow("patentType", "00")]
        [DataRow("fromRenewalYear", "00")]
        [DataRow("renewalYear", "00")]
        [DataRow("isLateGrant", "00")]
        [DataRow("isLOR", "00")]
        [DataRow("rightId", "01")]
        [DataRow("rightType", "01")]
        [DataRow("patentType", "01")]
        [DataRow("fromRenewalYear", "01")]
        [DataRow("renewalYear", "01")]
        [DataRow("isLateGrant", "01")]
        [DataRow("isLOR", "01")]
        [DataRow("rightId", "10")]
        [DataRow("rightType", "10")]
        [DataRow("patentType", "10")]
        [DataRow("fromRenewalYear", "10")]
        [DataRow("renewalYear", "10")]
        [DataRow("isLateGrant", "10")]
        [DataRow("isLOR", "10")]
        public async Task CalculateFeesAsyncThrowsAListOfErrorsWhenRenewalsRequiredValueIsMissing(string fieldName, string channel)
        {
            //Arrange
            Error.Add(Error.Create<RenewalsFeeValidator>(RenewalsFeeValidatorErrorCode));
            var feeCalculationRequest = new FeeCalculationRequest()
            {
                PaidDate = DateTime.Now.ToString("yyyy-MM-dd"),
                RequestDetails = new[] { Helper.BuildFeeCalculationRequestDetailsWithMissingField(fieldName) }
            };

            SetupServiceRequestAndProduct(ServiceRequestTypeEnum.Renewals, channel, "2023-01-01", string.Empty, string.Empty, 0.00M, string.Empty);

            //Act
            StatusCodeExceptionList exl = await Assert.ThrowsExactlyAsync<StatusCodeExceptionList>(() => _feeManagementService.CalculateFeesAsync(ServiceRequestTypeEnum.Renewals, feeCalculationRequest, channel));

            //Assert
            exl.StatusCode.Should().Be(StatusCodes.Status422UnprocessableEntity);
            exl.Errors.Should().NotBeEmpty();
            exl.Errors.Count.Should().Be(1);
            exl.Errors.FirstOrDefault()!.Code.Should().Be(RenewalsFeeValidatorErrorCode);
            var error = exl.Errors.FirstOrDefault();
            Assert.IsTrue(
                error?.Description != null &&
                error.Description.Contains("The request details could not be processed. The following mandatory field is missing: " + fieldName),
                "Expected error message for missing mandatory field was not found."
            );
        }

        [TestMethod]
        [DataRow("renewalDueDate", "00")]
        [DataRow("renewalDueDate", "01")]
        [DataRow("renewalDueDate", "10")]
        public async Task CalculateFeesAsyncThrowsAListOfErrorsWhenRenewalDueDateIsMissing(string fieldName, string channel)
        {
            //Arrange
            Error.Add(Error.Create<RenewalsFeeValidator>(RenewalsFeeValidatorErrorCode));
            var feeCalculationRequest = new FeeCalculationRequest()
            {
                PaidDate = DateTime.Now.ToString("yyyy-MM-dd"),
                RequestDetails = new[] { Helper.BuildFeeCalculationRequestDetailsWithMissingField(fieldName) }
            };

            SetupServiceRequestAndProduct(ServiceRequestTypeEnum.Renewals, channel, "2023-01-01", string.Empty, string.Empty, 0.00M, string.Empty);

            //Act
            StatusCodeExceptionList exl = await Assert.ThrowsExactlyAsync<StatusCodeExceptionList>(() => _feeManagementService.CalculateFeesAsync(ServiceRequestTypeEnum.Renewals, feeCalculationRequest, channel));

            //Assert
            exl.StatusCode.Should().Be(StatusCodes.Status422UnprocessableEntity);
            exl.Errors.Should().NotBeEmpty();
            exl.Errors.Count.Should().Be(1);
            exl.Errors.FirstOrDefault()!.Code.Should().Be(RenewalsFeeValidatorErrorCode);
            var error = exl.Errors.FirstOrDefault();
            Assert.IsTrue(
                error?.Description != null &&
                error.Description.Contains("The Renewal Due Date value '' could not be processed. A valid date must be provided and formatted as yyyy-MM-dd"),
                "Expected error message for missing mandatory field was not found."
            );
        }

        [TestMethod]
        [DataRow("", "00")]
        [DataRow(null, "00")]
        [DataRow(" ", "00")]
        [DataRow("", "01")]
        [DataRow(null, "01")]
        [DataRow(" ", "01")]
        [DataRow("", "10")]
        [DataRow(null, "10")]
        [DataRow(" ", "10")]
        public async Task CalculateFeesAsyncThrowsAListOfErrorsWhenRightTypeIsNullOrWhitespace(string rightType, string channel)
        {
            //Arrange
            Error.Add(Error.Create<RenewalsFeeValidator>(RenewalsFeeValidatorErrorCode));
            FeeCalculationRequest feeCalculationRequest = new()
            {
                PaidDate = DateTime.Now.ToString("yyyy-MM-dd"),
                RequestDetails = new[] { new FeeCalculationRequestDetails() {
                        RightId = 5,
                        RightType = rightType,
                        PatentType = "EP",
                        FromRenewalYear = 5,
                        RenewalYear = 5,
                        IsLateGrant = false,
                        IsLOR = false,
                        RenewalDueDate = "2023-04-16"
                    }
                }
            };

            SetupServiceRequestAndProduct(ServiceRequestTypeEnum.Renewals, channel, "2023-01-01", string.Empty, string.Empty, 0.00M, string.Empty);

            //Act
            StatusCodeExceptionList exl = await Assert.ThrowsExactlyAsync<StatusCodeExceptionList>(() => _feeManagementService.CalculateFeesAsync(ServiceRequestTypeEnum.Renewals, feeCalculationRequest, channel));

            //Assert
            exl.StatusCode.Should().Be(StatusCodes.Status422UnprocessableEntity);
            exl.Errors.Should().NotBeEmpty();
            exl.Errors.Count.Should().Be(1);
            exl.Errors.FirstOrDefault()!.Code.Should().Be(RenewalsFeeValidatorErrorCode);
            var error = exl.Errors.FirstOrDefault();
            Assert.IsTrue(
                error?.Description != null &&
                error.Description.Contains(@"The request details could not be processed. The following mandatory field is missing: rightType"),
                "Expected error message for rightType null or empty was not found."
            );
        }

        [TestMethod]
        [DataRow("00")]
        [DataRow("01")]
        [DataRow("10")]
        public async Task CalculateFeesAsyncThrowsAListOfErrorsWhenSecondRequestDetailsValuesIsNull(string channel)
        {
            //Arrange
            Error.Add(Error.Create<RenewalsFeeValidator>(RenewalsFeeValidatorErrorCode));
            FeeCalculationRequest feeCalculationRequest = new()
            {
                PaidDate = "2023-07-13",
                RequestDetails = GetMultipleFeeCalculationRequests()
            };
            feeCalculationRequest.RequestDetails[1].RightId = null;

            var serviceRequestType = ServiceRequestTypeEnum.Renewals;
            var referenceDate = DateTime.ParseExact("2023-01-01", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None);

            await Helper.CreateServiceRequestTypeInRepo(_feeDbRepository, 1, serviceRequestType.ToString());

            string gbProductCode = "P12-05";
            string gbRule = "IF((rightType=\"Patent\")*(patentType=\"GB\")*(fromRenewalYear<=5)*(renewalYear>=5)*(isLOR=0)*(MIN(paidDate,renewalDueDate)>=\"06/04/2018\"),1,0)";
            var gbPrice = 60.00M;
            await Helper.AddCompleteProductToServiceRequestType(_feeDbRepository, serviceRequestType.ToString(), channel, gbProductCode, "Patent Renewal-Year05", 0.00M, "OS", gbPrice, gbRule, referenceDate);

            //Act
            StatusCodeExceptionList exl = await Assert.ThrowsExactlyAsync<StatusCodeExceptionList>(() => _feeManagementService.CalculateFeesAsync(ServiceRequestTypeEnum.Renewals, feeCalculationRequest, channel));

            //Assert
            exl.StatusCode.Should().Be(StatusCodes.Status422UnprocessableEntity);
            exl.Errors.Should().NotBeEmpty();
            exl.Errors.Count.Should().Be(1);
            exl.Errors.FirstOrDefault()!.Code.Should().Be(RenewalsFeeValidatorErrorCode);
            var error = exl.Errors.FirstOrDefault();
            Assert.IsTrue(
                error?.Description != null &&
                error.Description.Contains(@"The request details could not be processed. The following mandatory field is missing: rightId"),
                "Expected error message for second request value is null was not found."
            );
        }

        private FeeCalculationRequestDetails[] GetMultipleFeeCalculationRequests()
        {
            var feeCalculationRequestDetails = new[] {
            new FeeCalculationRequestDetails()
            {
                RightId = 5,
                RightType = "Patent",
                PatentType = "EP",
                FromRenewalYear = 5,
                IsLateGrant = false,
                IsLOR = false,
                RenewalYear = 5,
                RenewalDueDate = "2023-04-16"
            },
            new FeeCalculationRequestDetails()
            {
                RightId = 6,
                RightType = "Patent",
                PatentType = "EP",
                FromRenewalYear = 5,
                IsLateGrant = true,
                IsLOR = false,
                RenewalYear = 5,
                RenewalDueDate = "2023-04-16"
            }
        };

            return feeCalculationRequestDetails;
        }
        #endregion

        #region Manage
        [TestMethod]
        [DataRow(ServiceRequestTypeEnum.ReferenceOfDisputeToComptroller, "Reference of dispute to Comptroller", "00")]
        [DataRow(ServiceRequestTypeEnum.ReferenceOfDisputeToComptroller, "Reference of dispute to Comptroller", "01")]
        [DataRow(ServiceRequestTypeEnum.ApplicationToBeMadeAPartyToProceedings, "Application to be made a party to proceedings", "00")]
        [DataRow(ServiceRequestTypeEnum.ApplicationToBeMadeAPartyToProceedings, "Application to be made a party to proceedings", "01")]
        [DataRow(ServiceRequestTypeEnum.ApplyToSettleOrAdjustLicenseOfRightTermsPreAugust1989, "Apply to settle/adjust license of right terms pre August 1989", "00")]
        [DataRow(ServiceRequestTypeEnum.ApplyToSettleOrAdjustLicenseOfRightTermsPreAugust1989, "Apply to settle/adjust license of right terms pre August 1989", "01")]
        [DataRow(ServiceRequestTypeEnum.VaryLicenceOfRightTermsByDesignRightOrCopyrightOwner, "Vary licence of right terms by design right or copyright owner", "00")]
        [DataRow(ServiceRequestTypeEnum.VaryLicenceOfRightTermsByDesignRightOrCopyrightOwner, "Vary licence of right terms by design right or copyright owner", "01")]
        [DataRow(ServiceRequestTypeEnum.NoticeOfOppositionToProceedingsBeforeTheComptroller, "Patent Notice of Opposition to proceedings before the Comptroller", "00")]
        [DataRow(ServiceRequestTypeEnum.NoticeOfOppositionToProceedingsBeforeTheComptroller, "Patent Notice of Opposition to proceedings before the Comptroller", "01")]
        [DataRow(ServiceRequestTypeEnum.RequestForOpinionAsToValidityOrInfringementOfAPatent, "Patent Request for Opinion as to validity or infringement of a patent", "00")]
        [DataRow(ServiceRequestTypeEnum.RequestForOpinionAsToValidityOrInfringementOfAPatent, "Patent Request for Opinion as to validity or infringement of a patent", "01")]
        [DataRow(ServiceRequestTypeEnum.InitiationOfProceedingsBeforeTheComptroller, "Patent InterParte Proceedings", "00")]
        [DataRow(ServiceRequestTypeEnum.InitiationOfProceedingsBeforeTheComptroller, "Patent InterParte Proceedings", "01")]
        [DataRow(ServiceRequestTypeEnum.ContinuationOfProceedingsBeforeTheComptroller, "Patent Continuation of Proceedings before the Comptroller", "00")]
        [DataRow(ServiceRequestTypeEnum.ContinuationOfProceedingsBeforeTheComptroller, "Patent Continuation of Proceedings before the Comptroller", "01")]
        [DataRow(ServiceRequestTypeEnum.ApplicationForDeclarationOfLapseOrInvalidityOrToRevokeAnExtensionOfTheDurationOfAnSPC, "SPC Application for declaration of lapse or invalidity or to revoke an extension of the duration of an SPC", "00")]
        [DataRow(ServiceRequestTypeEnum.ApplicationForDeclarationOfLapseOrInvalidityOrToRevokeAnExtensionOfTheDurationOfAnSPC, "SPC Application for declaration of lapse or invalidity or to revoke an extension of the duration of an SPC", "01")]
        [DataRow(ServiceRequestTypeEnum.LicenceOfRight, "Patent Licence of Right", "00")]
        [DataRow(ServiceRequestTypeEnum.LicenceOfRight, "Patent Licence of Right", "01")]
        [DataRow(ServiceRequestTypeEnum.CancellationOfLicenceOfRight, "Patent Cancellation of Licence of Right", "00")]
        [DataRow(ServiceRequestTypeEnum.CancellationOfLicenceOfRight, "Patent Cancellation of Licence of Right", "01")]
        [DataRow(ServiceRequestTypeEnum.ApplicationToRestoreAPatent, "Application to Restore a patent", "00")]
        [DataRow(ServiceRequestTypeEnum.ApplicationToRestoreAPatent, "Application to Restore a patent", "01")]
        [DataRow(ServiceRequestTypeEnum.SurrenderARight, "Surrender a Patent", "00")]
        [DataRow(ServiceRequestTypeEnum.SurrenderARight, "Surrender a Patent", "01")]
        public async Task CalculateFeesAsyncReturnsNoRuleQuantity(ServiceRequestTypeEnum serviceRequestType, string productName, string channel)
        {
            // Arrange
            string rule = Settings.NoQuantityRuleValue;
            SetupServiceRequestAndProduct(serviceRequestType, channel, "2023-01-01", string.Empty, productName, 0.00M, rule);

            FeeCalculationRequest feeCalculationRequest = new()
            {
                PaidDate = "2023-05-01",
                RequestDetails = new[] { new FeeCalculationRequestDetails() }
            };

            // Act
            var results = await _feeManagementService.CalculateFeesAsync(serviceRequestType, feeCalculationRequest, channel);

            // Assert
            results.Should().NotBeNull();
            results.ServiceRequestType.Should().Be(serviceRequestType.ToString());
            results.LineItems.Length.Should().Be(1);
            results.LineItems.First().ProductName.Should().Be(productName);
            results.LineItems.First().Quantity.Should().Be(1);
            results.CustomerChannel.Should().Be(channel);
        }

        [TestMethod]
        [DataRow(ServiceRequestTypeEnum.ChangeLicenceInterest, "Patent Change Licence Interest", "00")]
        [DataRow(ServiceRequestTypeEnum.ChangeLicenceInterest, "Change Owner or Give Notice of Rights", "01")]
        [DataRow(ServiceRequestTypeEnum.ChangeOwnerOrGiveNoticeOfRights, "Change Owner or Give Notice of Rights", "01")]
        [DataRow(ServiceRequestTypeEnum.ChangeSecurityInterest, "Patent Change Licence Interest", "00")]
        [DataRow(ServiceRequestTypeEnum.ChangeSecurityInterest, "Change Owner or Give Notice of Rights", "01")]
        public async Task CalculateFeesAsyncResultsNumberOfPatentQuantityRule(ServiceRequestTypeEnum serviceRequestType, string productName, string channel)
        {
            // Arrange
            string rule = "IF( numberOfPatents >= 1, 1, 0)";
            SetupServiceRequestAndProduct(serviceRequestType, channel, "2023-01-01", string.Empty, productName, 0.00M, rule);

            FeeCalculationRequest feeCalculationRequest = new()
            {
                PaidDate = "2023-05-01",
                RequestDetails = new[] { new FeeCalculationRequestDetails() {
                    NumberOfPatents = 25
                } }
            };

            // Act
            var results = await _feeManagementService.CalculateFeesAsync(serviceRequestType, feeCalculationRequest, channel);

            // Assert
            results.Should().NotBeNull();
            results.ServiceRequestType.Should().Be(serviceRequestType.ToString());
            results.LineItems.Length.Should().Be(1);
            results.LineItems.First().ProductName.Should().Be(productName);
            results.LineItems.First().Quantity.Should().Be(1);
            results.CustomerChannel.Should().Be(channel);
        }

        [TestMethod]
        [DataRow(ServiceRequestTypeEnum.UpdateNameOnARight, "Update Name on a Patent", "00")]
        [DataRow(ServiceRequestTypeEnum.UpdateNameOnARight, "Update Name on a Patent", "01")]
        [DataRow(ServiceRequestTypeEnum.UpdateAddressOnARight, "Update Address on a Patent", "00")]
        [DataRow(ServiceRequestTypeEnum.UpdateAddressOnARight, "Update Address on a Patent", "01")]
        [DataRow(ServiceRequestTypeEnum.ChangeOfRepresentative, "Patent Change of Representative", "00")]
        [DataRow(ServiceRequestTypeEnum.ChangeOfRepresentative, "Patent Change of Representative", "01")]
        public async Task ManageFeesReturnsRuleForNumberOfChanges(ServiceRequestTypeEnum serviceRequestType, string productName, string channel)
        {
            // Arrange
            string rule = "numberOfChanges";
            int expectedNumberOfChanges = 15;
            SetupServiceRequestAndProduct(serviceRequestType, channel, "2023-01-01", string.Empty, productName, 0.00M, rule);
            FeeCalculationRequest feeCalculationRequest = new()
            {
                PaidDate = "2023-05-01",
                RequestDetails = new[] { new FeeCalculationRequestDetails() {
                    NumberOfChanges = expectedNumberOfChanges
                } }
            };
            // Act
            var results = await _feeManagementService.CalculateFeesAsync(serviceRequestType, feeCalculationRequest, channel);
            // Assert
            results.Should().NotBeNull();
            results.ServiceRequestType.Should().Be(serviceRequestType.ToString());
            results.LineItems.Length.Should().Be(1);
            results.LineItems.First().ProductName.Should().Be(productName);
            results.CustomerChannel.Should().Be(channel);
            results.LineItems.First().Quantity.Should().Be(expectedNumberOfChanges);
        }
        #endregion

        #region Extension of Time
        [TestMethod]
        [DataRow("00", "P52", "Request to extend a prescribed deadline", 135.00, "IF((isSpecified = 0)*(isFoc = 0),1,0)", false, false, "135.00", 1, 1)]
        [DataRow("00", "P52", "Request to extend a prescribed deadline", 135.00, "IF((isSpecified = 0)*(isFoc = 0),1,0)", true, false, "0.00", 0, 0)]
        [DataRow("00", "P52", "Request to extend a prescribed deadline", 135.00, "IF((isSpecified = 0)*(isFoc = 0),1,0)", false, true, "0.00", 0, 0)]
        [DataRow("01", "P52", "Request to extend a prescribed deadline", 135.00, "IF((isSpecified = 0)*(isFoc = 0),1,0)", false, false, "135.00", 1, 1)]
        [DataRow("00", "N/A", "Request to extend a prescribed deadline - free of charge", 0.00, "IF((isSpecified = 0)*(isFoc = 1),1,0)", false, true, "0.00", 1, 1)]
        [DataRow("00", "N/A", "Request to extend a prescribed deadline - free of charge", 0.00, "IF((isSpecified = 0)*(isFoc = 1),1,0)", false, false, "0.00", 0, 0)]
        [DataRow("00", "N/A", "Request to extend a prescribed deadline - free of charge", 0.00, "IF((isSpecified = 0)*(isFoc = 1),1,0)", true, false, "0.00", 0, 0)]
        [DataRow("00", "N/A", "Request to extend a prescribed deadline - free of charge", 0.00, "IF((isSpecified = 0)*(isFoc = 1),1,0)", true, true, "0.00", 0, 0)]
        [DataRow("00", "N/A", "Request to extend a specified deadline", 0.00, "IF((isSpecified = 1),1,0)", true, false, "0.00", 1, 1)]
        [DataRow("00", "N/A", "Request to extend a specified deadline", 0.00, "IF((isSpecified = 1),1,0)", false, false, "0.00", 0, 0)]
        [DataRow("00", "N/A", "Request to extend a specified deadline", 0.00, "IF((isSpecified = 1),1,0)", false, true, "0.00", 0, 0)]
        [DataRow("00", "N/A", "Request to extend a specified deadline", 0.00, "IF((isSpecified = 1),1,0)", true, true, "0.00", 1, 1)]
        public async Task CalculateFeesAsyncResultsForExtensionOfTimeTest(string channel, string productCode, string productName, double price, string rule, bool isSpecified, bool isFoc, string expectedTotal, int expectedLineItems, int expectedQuantity)
        {
            // Arrange
            var serviceRequestType = ServiceRequestTypeEnum.ExtensionOfTime;

            SetupServiceRequestAndProduct(serviceRequestType, channel, DateTime.Now.AddMonths(-6).ToString("yyyy-MM-dd"), productCode, productName, (decimal)price, rule);

            FeeCalculationRequest feeCalculationRequest = new()
            {
                PaidDate = DateTime.Now.ToString("yyyy-MM-dd"),
                RequestDetails = new[]
                {
                    new FeeCalculationRequestDetails()
                    {
                        IsSpecified = isSpecified,
                        IsFoc = isFoc
                    }
                }
            };

            // Act
            var results = await _feeManagementService.CalculateFeesAsync(serviceRequestType, feeCalculationRequest, channel);

            // Assert
            results.ServiceRequestType.Should().Be(serviceRequestType.ToString());
            results.CustomerChannel.Should().Be(channel);
            results.Should().NotBeNull();
            results.TotalIncVat.Should().Be(decimal.Parse(expectedTotal));
            results.LineItems.Length.Should().Be(expectedLineItems);
            results.LineItems.Count(r => r.Quantity > 0).Should().Be(expectedQuantity);
        }
        #endregion

        #region ProductSequence
        [TestMethod]
        [DataRow("00", 1, 2, 3, ProductCodeA, ProductCodeB, 3, 1)]
        [DataRow("00", 3, 1, 2, ProductCodeB, ProductCodeC, 3, 1)]
        [DataRow("01", 3, 2, 1, ProductCodeC, ProductCodeB, 3, 1)]
        [DataRow("01", 3, 1, 1, ProductCodeB, ProductCodeA, 2, 2)]
        [DataRow("01", 5, 5, 1, ProductCodeC, ProductCodeA, 2, 1)]
        [DataRow("01", 5, 1, 5, ProductCodeB, ProductCodeA, 2, 1)]
        public async Task CalculateFeesAsyncResultsSortedByProductSequenceTest(string channel, int productASequence, int productBSequence, int productCSequence, string expectedProduct1, string expectedProduct2, int expectedProductCount, int expectedFirstQuantity)
        {
            // Arrange
            string referenceDate = DateTime.Now.ToString("yyyy-MM-dd");
            SetupServiceRequestAndProductWithProductSequence(channel, productASequence, productBSequence, productCSequence);

            FeeCalculationRequest feeCalculationRequest = new()
            {
                PaidDate = referenceDate,
                RequestDetails = new[] { new FeeCalculationRequestDetails() }
            };

            // Act
            var results = await _feeManagementService.CalculateFeesAsync(ServiceRequestTypeEnum.PatentApplicationSearchAndExam, feeCalculationRequest, channel);

            // Assert
            results.Should().NotBeNull();
            results.ServiceRequestType.Should().Be(ServiceRequestTypeEnum.PatentApplicationSearchAndExam.ToString());
            results.LineItems.Length.Should().Be(expectedProductCount);
            results.LineItems[0].E5ProductCode.Should().Be(expectedProduct1);
            results.LineItems[1].E5ProductCode.Should().Be(expectedProduct2);
            results.LineItems[0].LineNum.Should().Be(1);
            results.LineItems[1].LineNum.Should().Be(2);
            results.LineItems.First().Quantity.Should().Be(expectedFirstQuantity);
            results.CustomerChannel.Should().Be(channel);
        }

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
        #endregion

        #region PayForApplicationFee
        private async Task SetupPayForApplicationFeeDbValuesAsync(string channel)
        {
            var serviceRequestType = ServiceRequestTypeEnum.PayForApplicationFee;

            if (!_feeDbRepository.Context.ServiceRequestTypes!.Any(x => x.Name == serviceRequestType.ToString()))
            {
                await Helper.CreateServiceRequestTypeInRepo(_feeDbRepository, 56, serviceRequestType.ToString(), "C9999501");
            }

            var feeDetails = new List<(DateTime EffectiveFrom, DateTime? EffectiveTo, decimal PriceExVat)>
            {
                (new DateTime(2018, 04, 06), new DateTime(2026, 03, 31), channel == "00" ? 75.00M : 112.50M),
                (new DateTime(2026, 04, 01), null, channel == "00" ? 95.00M : 150.00M)
            };

            await Helper.AddCompleteProductToServiceRequestTypeMultipleFees(
                _feeDbRepository,
                serviceRequestType.ToString(),
                channel,
                "AF1",
                "Application fee after filing",
                0.00M,
                "OS",
                "IF((isApplicationFeePaid = 0),1,0)",
                feeDetails,
                productNumber: 1,
                productSequence: 1);
        }

        [TestMethod]
        [DataRow("00", "2018-04-06", 75.00)]
        [DataRow("00", "2024-05-13", 75.00)]
        [DataRow("00", "2026-03-31", 75.00)]
        [DataRow("00", "2026-04-01", 95.00)]
        [DataRow("00", "2026-10-05", 95.00)]
        [DataRow("01", "2018-04-06", 112.50)]
        [DataRow("01", "2024-05-13", 112.50)]
        [DataRow("01", "2026-03-31", 112.50)]
        [DataRow("01", "2026-04-01", 150.00)]
        [DataRow("01", "2026-10-05", 150.00)]
        public async Task CalculateFeesAsync_PayForApplicationFee_ReturnsCorrectFeeForDateAndChannel(string channel, string paidDate, double expectedFee)
        {
            // Arrange
            await SetupPayForApplicationFeeDbValuesAsync(channel);

            FeeCalculationRequest request = new()
            {
                PaidDate = paidDate,
                RequestDetails = new[] { new FeeCalculationRequestDetails() }
            };

            // Act
            var results = await _feeManagementService.CalculateFeesAsync(ServiceRequestTypeEnum.PayForApplicationFee, request, channel);

            // Assert
            results.Should().NotBeNull();
            results.ServiceRequestType.Should().Be(ServiceRequestTypeEnum.PayForApplicationFee.ToString());
            results.CustomerChannel.Should().Be(channel);
            results.TotalIncVat.Should().Be((decimal)expectedFee);
            results.LineItems.Length.Should().Be(1);
            results.LineItems[0].E5ProductCode.Should().Be("AF1");
            results.LineItems[0].Price.Should().Be((decimal)expectedFee);
            results.LineItems[0].Quantity.Should().Be(1);
        }

        [TestMethod]
        [DataRow("00", "2018-04-05")]
        [DataRow("01", "2018-04-05")]
        [DataRow("00", "2015-01-01")]
        [DataRow("01", "2015-01-01")]
        public async Task CalculateFeesAsync_PayForApplicationFee_BeforeApril2018_ThrowsNotFound(string channel, string paidDate)
        {
            // Arrange
            await SetupPayForApplicationFeeDbValuesAsync(channel);

            FeeCalculationRequest request = new()
            {
                PaidDate = paidDate,
                RequestDetails = new[] { new FeeCalculationRequestDetails() }
            };

            // Act
            var ex = await Assert.ThrowsExactlyAsync<StatusCodeException>(() =>
                _feeManagementService.CalculateFeesAsync(ServiceRequestTypeEnum.PayForApplicationFee, request, channel));

            // Assert
            ex.StatusCode.Should().Be(StatusCodes.Status404NotFound);
            ex.Message.Should().Be("Unable to find data for the given request. A fee could not be calculated.");
        }

        [TestMethod]
        [DataRow("00", true, 0.00)]
        [DataRow("00", false, 75.00)]
        [DataRow("01", true, 0.00)]
        [DataRow("01", false, 112.50)]
        public async Task CalculateFeesAsync_PayForApplicationFee_ReturnsCorrectFeeWhenApplicationFeePaidIsSet(string channel, bool isApplicationFeePaid, double expectedFee)
        {
            // Arrange
            await SetupPayForApplicationFeeDbValuesAsync(channel);

            FeeCalculationRequest request = new()
            {
                PaidDate = "2024-05-13",
                RequestDetails = new[] { new FeeCalculationRequestDetails() { IsApplicationFeePaid = isApplicationFeePaid } }
            };

            // Act
            var results = await _feeManagementService.CalculateFeesAsync(ServiceRequestTypeEnum.PayForApplicationFee, request, channel);

            // Assert
            results.Should().NotBeNull();
            results.ServiceRequestType.Should().Be(ServiceRequestTypeEnum.PayForApplicationFee.ToString());
            results.CustomerChannel.Should().Be(channel);
            results.TotalIncVat.Should().Be((decimal)expectedFee);

            if (expectedFee > 0)
            {
                results.LineItems.Length.Should().Be(1);
                results.LineItems[0].Price.Should().Be((decimal)expectedFee);
            }
            else
            {
                results.LineItems.Length.Should().Be(0);
            }
        }
        #endregion

        #region RetrieveFeeInformation


        #region Full Validation Method Tests

        [TestMethod]
        [DataRow("2024-05-01", "00", "1,2, 197", 200)]
        [DataRow("2025-09-01", "00", "1, 197", 200)]
        [DataRow("2099-01-01", "00", "1, 197", 200)]
        [DataRow("2025-01-01", "00100", "1, 197", 422)] //Invalid channel
        [DataRow("20254-01-01", "00", "1, 2, 197", 422)] //Invalid date
        [DataRow("2025-01-01", "00", "-1, 197", 422)] //Invalid product numbers
        [DataRow("2025-01-01", "00", "1, 3, 197", 404)] //Product code not in DB
        [DataRow("2025-01-01", "01", "1, 197", 404)] //Product code with Channel not in DB
        [DataRow("2010-01-01", "00", "1, 197", 404)] //Product code with Channel and Date not in DB
        public async Task RetrieveFeeInformation_HandlesValidAndInvalidInputsCorrectly(string effectiveDate, string channel, string productNumbers, int expectedStatusCode)
        {
            //Arrange
            var feeDetails = await SetupProductsInMemoryDatabaseAsync();

            var feeInformationRequest = new FeeInformationRequest();
            feeInformationRequest.EffectiveDate = effectiveDate;
            feeInformationRequest.ProductNumbers = productNumbers.Split(',').Select(int.Parse).ToArray();

            try
            {
                //Act
                var result = await _feeManagementService.RetrieveFeeInformation(feeInformationRequest, channel);
                var expectedTotalPriceExVat = feeDetails
                    .FirstOrDefault(f => DateTime.Parse(effectiveDate) >= f.EffectiveFrom &&
                        (f.EffectiveTo == null || DateTime.Parse(effectiveDate) <= f.EffectiveTo))
                    .PriceExVat;

                //Success Assert
                Assert.IsNotNull(result);
                Assert.HasCount(result.Products.Count, feeInformationRequest.ProductNumbers);
                Assert.AreEqual(effectiveDate, result.EffectiveDate);
                Assert.AreEqual(expectedTotalPriceExVat * feeInformationRequest.ProductNumbers.Count(), result.TotalRecentExVat); // Can just multiply by ProductNumber.count as they use the same price
                result.Products.Select(p => p.Number).ToList().Should().BeInAscendingOrder();

            }
            catch (StatusCodeException ex)
            {
                //Error Assert
                Assert.AreEqual(expectedStatusCode, ex.StatusCode);
            }
        }

        [TestMethod]
        public async Task FeeInformationResult_ShouldMatchExpectedObject()
        {
            //Arrange
            var effectiveDate = "2024-05-01";
            var channel = "00";
            var productNumbers = "1,2, 197";

            FeeInformationResult expectedResult = BuildExpectedFeeInformationResult();
            var feeDetails = await SetupProductsInMemoryDatabaseAsync();

            //Act
            var feeInformationRequest = new FeeInformationRequest();
            feeInformationRequest.EffectiveDate = effectiveDate;
            feeInformationRequest.ProductNumbers = productNumbers.Split(',').Select(int.Parse).ToArray();
            var result = await _feeManagementService.RetrieveFeeInformation(feeInformationRequest, channel);


            //Assert
            result.Should().BeEquivalentTo(expectedResult, options => options
                .WithStrictOrdering()
                .Using<decimal>(ctx => ctx.Subject.Should().BeApproximately(ctx.Expectation, 0.0001m))
                .WhenTypeIs<decimal>());

        }

        #endregion


        #region Individual Validation Method Tests
        [TestMethod]
        public void CheckAllProductNumbersExistInDB_ShouldNotThrow_WhenAllExist()
        {
            //Arrange
            var products = new List<ProductInformationResult>
            {
                BuildExpectedProductResult(1, "00", DateTime.Today.AddDays(-1), DateTime.Today.AddDays(1)),
                BuildExpectedProductResult(2, "00", DateTime.Today.AddDays(-1), DateTime.Today.AddDays(1))
            };
            var productNumbers = new[] { 1, 2 };

            //Act
            Action act = () => _feeManagementService.ValidateAllProductNumbersExist(products, productNumbers);

            //Assert
            act.Should().NotThrow();
        }

        [TestMethod]
        public void CheckAllProductNumbersExistInDB_ShouldThrow_WhenMissing()
        {
            //Arrange
            var products = new List<ProductInformationResult>
            {
                BuildExpectedProductResult(1, "00", DateTime.Today.AddDays(-1), DateTime.Today.AddDays(1))
            };
            var productNumbers = new[] { 1, 2 };

            //Act
            var ex = Assert.ThrowsExactly<StatusCodeException>(() =>
                _feeManagementService.ValidateAllProductNumbersExist(products, productNumbers));

            //Assert
            ex.StatusCode.Should().Be(StatusCodes.Status404NotFound);
        }

        [TestMethod]
        public void CheckIfChannelFiltersOutAnyProductDetails_ShouldReturnFilteredList_WhenChannelMatches()
        {
            //Arrange
            var products = new List<ProductInformationResult>
            {
                BuildExpectedProductResult(1, "00", DateTime.Today.AddDays(-1), DateTime.Today.AddDays(1)),
                BuildExpectedProductResult(2, "01", DateTime.Today.AddDays(-1), DateTime.Today.AddDays(1))
            };
            var productNumbers = new[] { 1 };

            //Act
            var result = _feeManagementService.FilterProductsByChannelAndValidate(products, productNumbers, "00");

            //Assert
            result.Count.Should().Be(1);
            result[0].Number.Should().Be(1);
        }

        [TestMethod]
        public void CheckIfChannelFiltersOutAnyProductDetails_ShouldThrow_WhenChannelFiltersOutNumbers()
        {
            //Arrange
            var products = new List<ProductInformationResult>
            {
                BuildExpectedProductResult(1, "01", DateTime.Today.AddDays(-1), DateTime.Today.AddDays(1))
            };
            var productNumbers = new[] { 1 };

            //Act
            var ex = Assert.ThrowsExactly<StatusCodeException>(() =>
                _feeManagementService.FilterProductsByChannelAndValidate(products, productNumbers, "00"));

            //Assert
            ex.StatusCode.Should().Be(StatusCodes.Status404NotFound);
        }

        [TestMethod]
        public void CheckIfDateFiltersOutAnyProductDetails_ShouldReturnFilteredList_WhenDateValid()
        {
            //Arrange
            var products = new List<ProductInformationResult>
            {
                BuildExpectedProductResult(1, "00", new DateTime(2024, 1, 1), new DateTime(2025, 1, 1))
            };
            var productNumbers = new[] { 1 };

            //Act
            var result = _feeManagementService.FilterProductsByDateAndValidate(products, productNumbers, new DateTime(2024, 6, 1));

            //Assert
            result.Count.Should().Be(1);
            result[0].Number.Should().Be(1);
        }

        [TestMethod]
        public void CheckIfDateFiltersOutAnyProductDetails_ShouldThrow_WhenDateFiltersOutNumbers()
        {
            //Arrange
            var products = new List<ProductInformationResult>
            {
                BuildExpectedProductResult(1, "00", new DateTime(2024, 1, 1), new DateTime(2024, 6, 1))
            };
            var productNumbers = new[] { 1 };

            //Act
            var ex = Assert.ThrowsExactly<StatusCodeException>(() =>
                _feeManagementService.FilterProductsByDateAndValidate(products, productNumbers, new DateTime(2025, 1, 1)));

            //Assert
            ex.StatusCode.Should().Be(StatusCodes.Status404NotFound);
        }




        [TestMethod]
        [DataRow("00")]
        [DataRow("01")]
        public void ValidateChannel_ShouldNotThrow_WhenChannelIsSupported(string channel)
        {
            //Act
            Action act = () => _feeManagementService.ValidateChannel(channel);
            //Assert
            act.Should().NotThrow();
        }

        [TestMethod]
        [DataRow("99", StatusCodes.Status422UnprocessableEntity)]
        [DataRow("ABC", StatusCodes.Status422UnprocessableEntity)]
        public void ValidateChannel_ShouldThrow_WhenChannelIsNotSupported(string channel, int expectedStatusCode)
        {
            //Act
            var ex = Assert.ThrowsExactly<StatusCodeException>(() => _feeManagementService.ValidateChannel(channel));
            //Assert
            ex.StatusCode.Should().Be(expectedStatusCode);
        }


        [TestMethod]
        [DataRow("2020-05-01", 2020, 5, 1)]
        [DataRow("2015-12-31", 2015, 12, 31)]
        public void ValidateAndReturnDate_ShouldReturnParsedDate_WhenValid(string date, int year, int month, int day)
        {
            //Act
            var result = _feeManagementService.ValidateAndReturnDate(date);
            //Assert
            result.Should().Be(new DateTime(year, month, day));
        }

        [TestMethod]
        [DataRow("05/01/2020", StatusCodes.Status422UnprocessableEntity)]
        [DataRow("2020-13-01", StatusCodes.Status422UnprocessableEntity)]
        public void ValidateAndReturnDate_ShouldThrow_WhenInvalidFormat(string date, int expectedStatusCode)
        {
            //Act
            var ex = Assert.ThrowsExactly<StatusCodeException>(() => _feeManagementService.ValidateAndReturnDate(date));
            //Assert
            ex.StatusCode.Should().Be(expectedStatusCode);
        }

        [TestMethod]
        [DataRow(new int[] { 1, 2, 3 })]
        [DataRow(new int[] { 10, 20 })]
        public void ValidateProductNumbers_ShouldReturnArray_WhenValid(int[] productNumbers)
        {
            //Act
            var result = _feeManagementService.ValidateProductNumbers(productNumbers);
            //Assert
            result.Should().Equal(productNumbers);
        }

        [TestMethod]
        [DataRow(null, StatusCodes.Status422UnprocessableEntity)]
        [DataRow(new int[] { }, StatusCodes.Status422UnprocessableEntity)]
        public void ValidateProductNumbers_ShouldThrow_WhenNullOrEmpty(int[]? productNumbers, int expectedStatusCode)
        {
            //Act
            var ex = Assert.ThrowsExactly<StatusCodeException>(() => _feeManagementService.ValidateProductNumbers(productNumbers));
            //Assert
            ex.StatusCode.Should().Be(expectedStatusCode);
        }

        [TestMethod]
        [DataRow(new int[] { -1, 2, 3 }, StatusCodes.Status422UnprocessableEntity)]
        [DataRow(new int[] { 0, 5 }, StatusCodes.Status422UnprocessableEntity)]
        public void ValidateProductNumbers_ShouldThrow_WhenContainsInvalidNumbers(int[] productNumbers, int expectedStatusCode)
        {
            //Act
            var ex = Assert.ThrowsExactly<StatusCodeException>(() => _feeManagementService.ValidateProductNumbers(productNumbers));
            //Assert
            ex.StatusCode.Should().Be(expectedStatusCode);
        }

        [TestMethod]
        [DataRow("2", true)]
        [DataRow("-2", true)]
        [DataRow("0", false)] 
        public void CheckDateModifier_ReturnCorrectBooleanValue(string daysToModifyString, bool expectedResult)
        {
            //Arrange
            _feeConfiguration.SetupGet(x => x[It.Is<string>(s => s == "DaysToModifyDate")]).Returns(daysToModifyString);
            //Act
            var result = _feeManagementService.CheckDateModifier(out int daysToModify);
            //Assert
            result.Should().Be(expectedResult);
            daysToModify.Should().Be(Int32.Parse(daysToModifyString));
        }

        [TestMethod]
        [DataRow("2026-01-01", "2026-01-01", "0")]  //ReturnOriginalPaidDate
        [DataRow("2026-01-01", "2026-01-03", "2")]  //ReturnModifiedPaidDateTwoDaysAhead
        [DataRow("2026-01-31", "2026-02-02", "2")]  //ReturnModifiedPaidDateInNextMonth
        [DataRow("2025-12-31", "2026-01-02", "2")]  //ReturnModifiedPaidDateInNextYear
        [DataRow("2026-01-03", "2026-01-01", "-2")] //ReturnModifiedPaidDateTwoDaysBehind
        [DataRow("2026-02-02", "2026-01-31", "-2")] //ReturnModifiedPaidDateInPreviousMonth
        [DataRow("2026-01-02", "2025-12-31", "-2")] //ReturnModifiedPaidDateInPreviousYear
        public async Task CalculateFeesAsync_ShouldSetPaidDateCorrectlyBasedOnDaysToModifyDateValue(string originalDateString, string finalModifiedDate, string daysToModifyDate)
        {
            // Arrange
            var serviceRequestType = ServiceRequestTypeEnum.Acceleration;

            string productName = "Request to accelerate application";
            decimal price = 20.00M;
            SetupServiceRequestAndProduct(serviceRequestType, "01", "2026-01-01", string.Empty, productName, price, "numberOfCertifiedCopies");

            var requestDetails = new FeeCalculationRequestDetails();
            var propertyInfo = requestDetails.GetType().GetProperty("NumberOfCertifiedCopies");
            propertyInfo!.SetValue(requestDetails, 10, null);

            _feeConfiguration.SetupGet(x => x[It.Is<string>(s => s == "DaysToModifyDate")]).Returns(daysToModifyDate);

            FeeCalculationRequest feeCalculationRequest = new()
            {
                PaidDate = originalDateString,
                RequestDetails = new[] { requestDetails }
            };

            // Act
            var results = await _feeManagementService.CalculateFeesAsync(ServiceRequestTypeEnum.Acceleration, feeCalculationRequest, "01");

            // Assert
            results.Should().NotBeNull();
            feeCalculationRequest.PaidDate.Should().Be(finalModifiedDate);
        }

        [TestMethod]
        [DataRow("2026-01-01", "2026-01-01", "0")]  //ReturnOriginalEffectiveDate
        [DataRow("2026-01-01", "2026-01-03", "2")]  //ReturnModifiedEffectiveDateTwoDaysAhead
        [DataRow("2026-01-31", "2026-02-02", "2")]  //ReturnModifiedEffectiveDateInNextMonth
        [DataRow("2025-12-31", "2026-01-02", "2")]  //ReturnModifiedEffectiveDateInNextYear
        [DataRow("2026-01-03", "2026-01-01", "-2")] //ReturnModifiedEffectiveDateTwoDaysBehind
        [DataRow("2026-02-02", "2026-01-31", "-2")] //ReturnModifiedEffectiveDateInPreviousMonth
        [DataRow("2026-01-02", "2025-12-31", "-2")] //ReturnModifiedEffectiveDateInPreviousYear
        public async Task RetrieveFeeInformation_ShouldSetEffectiveDateCorrectlyBasedOnDaysToModifyDateValue(string originalDateString, string finalModifiedDate, string daysToModifyDate)
        {
            //Arrange
            _feeConfiguration.SetupGet(x => x[It.Is<string>(s => s == "DaysToModifyDate")]).Returns(daysToModifyDate);         
            var channel = "00";
            var productNumbers = "1,2, 197";

            FeeInformationResult expectedResult = BuildExpectedFeeInformationResult();
            var feeDetails = await SetupProductsInMemoryDatabaseAsync();

            //Act
            var feeInformationRequest = new FeeInformationRequest();
            feeInformationRequest.EffectiveDate = originalDateString;
            feeInformationRequest.ProductNumbers = productNumbers.Split(',').Select(int.Parse).ToArray();
            var result = await _feeManagementService.RetrieveFeeInformation(feeInformationRequest, channel);

            // Assert
            result.Should().NotBeNull();
            feeInformationRequest.EffectiveDate.Should().Be(finalModifiedDate);
        }
        #endregion

        #region Set up prerequisites

        private async Task<List<(DateTime EffectiveFrom, DateTime? EffectiveTo, decimal PriceExVat)>> SetupProductsInMemoryDatabaseAsync()
        {
            string rule = Settings.NoQuantityRuleValue;
            DateTime referenceDate = DateTime.Now;

            var feeDetails = new List<(DateTime EffectiveFrom, DateTime? EffectiveTo, decimal PriceExVat)>
            {
                (new DateTime(2024,04,01),new DateTime(2025,03,31), 120m),
                (new DateTime(2025,04,01),new DateTime(2026,03,31), 140m),
                (new DateTime(2026,04,01),null, 150m)
            };

            await Helper.CreateServiceRequestTypeInRepo(_feeDbRepository, 1, ServiceRequestTypeEnum.PatentSearch.ToString(), "e5PatentSearch");
            await Helper.CreateServiceRequestTypeInRepo(_feeDbRepository, 2, ServiceRequestTypeEnum.ReferenceOfDisputeToComptroller.ToString(), "e5AReferenceOfDisputeToComptroller");
            await Helper.CreateServiceRequestTypeInRepo(_feeDbRepository, 3, ServiceRequestTypeEnum.PcteFilingReductionFull.ToString(), "e5PcteFilingReductionFull");
            await Helper.AddCompleteProductToServiceRequestTypeMultipleFees(_feeDbRepository, ServiceRequestTypeEnum.PcteFilingReductionFull.ToString(), "00", "PCT-XML", "International patent application - XML", 0.00M, "OS", rule, feeDetails, 197, 1);
            await Helper.AddCompleteProductToServiceRequestTypeMultipleFees(_feeDbRepository, ServiceRequestTypeEnum.PatentSearch.ToString(), "00", "AF1", "Application fee after filing", 0.00M, "OS", rule, feeDetails, 1, 1);
            await Helper.AddCompleteProductToServiceRequestTypeMultipleFees(_feeDbRepository, ServiceRequestTypeEnum.PcteFilingReductionFull.ToString(), "01", "PCT-XML", "International patent application - XML", 0.00M, "OS", rule, feeDetails, 197, 1);
            await Helper.AddCompleteProductToServiceRequestTypeMultipleFees(_feeDbRepository, ServiceRequestTypeEnum.ReferenceOfDisputeToComptroller.ToString(), "00", "DRF1", "Reference of a dispute to Comptroller", 0.00M, "OS", rule, feeDetails, 2, 1);

            return feeDetails;
        }

        
        private static FeeInformationResult BuildExpectedFeeInformationResult()
        {
            var expectedProducts = new List<ProductInformationResult>
            {
                new ProductInformationResult(
                    1, "00", "Application fee after filing", "AF1", 120m, 0m,
                    new List<ProductInformationResultDetails>
                    {
                        new ProductInformationResultDetails(
                            new ServiceRequestTypeResult(new ServiceRequestType { Name = "PatentSearch", E5AccountNumber = "e5PatentSearch" }),
                            new ProductServiceRequestTypeResult(new ProductServiceRequestType { ProductSequence = 1 }),
                            new FeeResult(new Fee { PriceExVat = 120m, Rule = "No Rule Effective Fee = 1", EffectiveFrom = new DateTime(2024,04,01), EffectiveTo = new DateTime(2025,03,31) }, 0m),
                            new TaxCodeResult(new TaxCode { Code = "OS", Description = null, Rate = 0m, EffectiveFrom = new DateTime(2024,04,01), EffectiveTo = null }),
                            new QuantityRulesResult(new QuantityRule { Rule = "No Rule Quantity = 1", EffectiveFrom = new DateTime(2024,04,01), EffectiveTo = null })
                        )
                    }
                ),
                new ProductInformationResult(
                    2, "00", "Reference of a dispute to Comptroller", "DRF1", 120m, 0m,
                    new List<ProductInformationResultDetails>
                    {
                        new ProductInformationResultDetails(
                            new ServiceRequestTypeResult(new ServiceRequestType { Name = "ReferenceOfDisputeToComptroller", E5AccountNumber = "e5AReferenceOfDisputeToComptroller" }),
                            new ProductServiceRequestTypeResult(new ProductServiceRequestType { ProductSequence = 1 }),
                            new FeeResult(new Fee { PriceExVat = 120m, Rule = "No Rule Effective Fee = 1", EffectiveFrom = new DateTime(2024,04,01), EffectiveTo = new DateTime(2025,03,31) }, 0m),
                            new TaxCodeResult(new TaxCode { Code = "OS", Description = null, Rate = 0m, EffectiveFrom = new DateTime(2024,04,01), EffectiveTo = null }),
                            new QuantityRulesResult(new QuantityRule { Rule = "No Rule Quantity = 1", EffectiveFrom = new DateTime(2024,04,01), EffectiveTo = null })
                        )
                    }
                ),
                new ProductInformationResult(
                    197, "00", "International patent application - XML", "PCT-XML", 120m, 0m,
                    new List<ProductInformationResultDetails>
                    {
                        new ProductInformationResultDetails(
                            new ServiceRequestTypeResult(new ServiceRequestType { Name = "PcteFilingReductionFull", E5AccountNumber = "e5PcteFilingReductionFull" }),
                            new ProductServiceRequestTypeResult(new ProductServiceRequestType { ProductSequence = 1 }),
                            new FeeResult(new Fee { PriceExVat = 120m,  Rule = "No Rule Effective Fee = 1", EffectiveFrom = new DateTime(2024,04,01), EffectiveTo = new DateTime(2025,03,31) }, 0m),
                            new TaxCodeResult(new TaxCode { Code = "OS", Description = null, Rate = 0m, EffectiveFrom = new DateTime(2024,04,01), EffectiveTo = null }),
                            new QuantityRulesResult(new QuantityRule { Rule = "No Rule Quantity = 1", EffectiveFrom = new DateTime(2024,04,01), EffectiveTo = null })
                        )
                    }
                )
            };
            var expectedResult = new FeeInformationResult("2024-05-01", "00", 360m, 0m, 360m, expectedProducts);
            return expectedResult;
        }



        private ProductInformationResult BuildExpectedProductResult(int number, string channel, DateTime from, DateTime? to)
        {
            var details = new List<ProductInformationResultDetails>
            {
                new ProductInformationResultDetails(
                    new ServiceRequestTypeResult(new ServiceRequestType { Name = "Test", E5AccountNumber = "acc" }),
                    new ProductServiceRequestTypeResult(new ProductServiceRequestType { ProductSequence = 1 }),
                    new FeeResult(new Fee { PriceExVat = 100m, Rule = "Rule", EffectiveFrom = from, EffectiveTo = to }, 0m),
                    new TaxCodeResult(new TaxCode { Code = "OS", Description = null, Rate = 0m, EffectiveFrom = from, EffectiveTo = to }),
                    new QuantityRulesResult(new QuantityRule { Rule = "Rule", EffectiveFrom = from, EffectiveTo = to })
                )
            };

            return new ProductInformationResult(number, channel, $"Product {number}", $"Code{number}", 100m, 0m, details);
        }

        #endregion

        #endregion
    }



}

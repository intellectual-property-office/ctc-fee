using AutoFixture;
using AwesomeAssertions;
using IPO.FeeService.API.Controllers;
using IPO.FeeService.Interfaces;
using IPO.FeeService.Models.API;
using IPO.FeeService.Models.Constants;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Net;
using System.Threading.Tasks;

namespace IPO.FeeService.UnitTests.API
{
    [TestClass]
    public class FeeControllerTests
    {
        private readonly Mock<IFeeManagementService> _mockFeeManagementService;
        private readonly Fixture _fixture;

        public FeeControllerTests()
        {
            _mockFeeManagementService = new Mock<IFeeManagementService>();
            _fixture = new Fixture();
        }

        [TestMethod]
        public async Task CalculateFeeReturnsAcceptedAndCorrectResults()
        {
            // Arrange
            var testResults = _fixture.Build<FeeCalculationResults>().Create();

            _mockFeeManagementService
                .Setup(s => s.CalculateFeesAsync(It.IsAny<ServiceRequestTypeEnum>(), It.IsAny<FeeCalculationRequest>(), It.IsAny<string>()))
                .ReturnsAsync(testResults)
                .Verifiable();

            var feeApi = new FeeController(_mockFeeManagementService.Object);

            FeeCalculationRequest feeCalculationRequest = new()
            {
                PaidDate = DateTime.Now.ToString("yyyy-MM-dd"),
                RequestDetails = new[]
                {
                    new FeeCalculationRequestDetails()
                    {
                        NumberOfDesigns = 1,
                        NumberOfPatents = 1,
                        NumberOfTrademarks = 1
                    }
                }
            };

            // Act
            var feeRequest = await feeApi.CalculateFee(feeCalculationRequest, ServiceRequestTypeEnum.TransferOfOwner, "00");
            var feeResult = (OkObjectResult)feeRequest.Result!;
            var results = (FeeCalculationResults)feeResult.Value!;

            // Assert
            results.Should().Be(testResults);
            feeResult.StatusCode.Should().NotBeNull();
            feeResult.StatusCode.Should().Be((int)HttpStatusCode.OK);
            _mockFeeManagementService.Verify();
        }
    }
}

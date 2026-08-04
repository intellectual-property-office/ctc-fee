using AutoFixture;
using AutoFixture.Kernel;
using FluentAssertions;
using IPO.Common.Infrastructure;
using IPO.Common.ServiceRequest.Models;
using IPO.Common.ServiceRequest.Models.TransferOfOwnership;
using IPO.FeeService.Services.Calculating;
using IPO.FeeService.Services.Calculating.Calculators;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace IPO.FeeService.UnitTests.Services
{
    [TestClass]
    public class FeeCalculationServiceTests
    {
        private Fixture _fixture;
        private IFeeCalculationService _feeCalculationProvider;
        public FeeCalculationServiceTests()
        {
            _fixture = new Fixture();
            _fixture.Customizations.Add(new TypeRelay(typeof(IFeeCalculator), typeof(TransferOfOwnershipCalculator)));
        }

        [TestMethod]
        public void CalculateFeeWhenRequestIsNullThrowsInvalidCastException()
        {
            // Arrange
            Error.Add(Error.Create<FeeCalculationService>("E-002"));
            var testedMissingCalculatorType = ServiceRequestType.TransferOfOwnership;
            var serviceRequest = _fixture.Build<ServiceRequest>()
                                              .With(o => o.Header,
                                                  _fixture.Build<RequestHeader>()
                                                  .With(o => o.Type, testedMissingCalculatorType)
                                                  .Create())
                                              .With(o => o.Details, _fixture.Build<TransferOfOwnershipRequest>().Create())
                .Create();
            var feeCalculators = FeeCalculatorFactory.GetFeeCalculators().Where(o => o.RequestType != testedMissingCalculatorType);
            _feeCalculationProvider = new FeeCalculationService(feeCalculators);

            // Act  
            var result = ((IFeeCalculationService)_feeCalculationProvider).Invoking(o => o.CalculateFee(serviceRequest));

            // Assert 
            result.Should().Throw<StatusCodeException>().Which.StatusCode.Should().Be(500);
        }

        [TestMethod]
        public void CalculateFeeWhenRequestIsNotNullReturnsFee()
        {
            //Arrange
            Error.Add(Error.Create<FeeCalculationService>("E-003"));
            var testedMissingCalculatorType = ServiceRequestType.TransferOfOwnership;
            var serviceRequest = _fixture.Build<ServiceRequest>()
                                              .With(o => o.Header,
                                                  _fixture.Build<RequestHeader>()
                                                  .With(o => o.Type, testedMissingCalculatorType)
                                                  .Create())
                                              .With(o => o.Details, _fixture.Build<TransferOfOwnershipRequest>().Create())
                .Create();
            var feeCalculators = FeeCalculatorFactory.GetFeeCalculators();
            _feeCalculationProvider = new FeeCalculationService(feeCalculators);

            //Act
            var result = _feeCalculationProvider.CalculateFee(serviceRequest);

            //Assert
            result.Should().BeGreaterThanOrEqualTo(0);
        }
    }
}

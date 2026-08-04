using AutoFixture;
using FluentAssertions;
using IPO.Common.ServiceRequest.Models;
using IPO.Common.ServiceRequest.Models.PatentApply;
using IPO.FeeService.Services.Calculating.Calculators;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IPO.FeeService.UnitTests.Services
{
    [TestClass]
    public class PatentApplyCalculatorTests
    {
        private readonly Fixture _fixture;
        private readonly PatentApplyCalculator _PatentApplyCalculator;
        private readonly ServiceRequest _serviceRequest;
        public PatentApplyCalculatorTests()
        {
            this._fixture = new Fixture();
            this._PatentApplyCalculator = new PatentApplyCalculator();
            this._serviceRequest = this._fixture.Build<ServiceRequest>()
                                    .With(o => o.Header,
                                    this._fixture.Build<RequestHeader>().With(o => o.Type,
                                    ServiceRequestType.PatentApply).Create())
                                    .With(o => o.Details, this._fixture.Create<PatentApplyRequest>())
                                    .Create();
        }

        [TestMethod]
        public void CalculatorContainsRightRequestType()
        {
            // Arrange    
            var expectedType = ServiceRequestType.PatentApply;

            // Act   
            var result = this._PatentApplyCalculator.RequestType;

            // Assert  
            result.Should().Be(expectedType);
        }

        [TestMethod]
        public void CalculatorCalculateReturnsRightAmount()
        {
            //Arrange
            var expectedResult = 0;

            //Act
            var result = this._PatentApplyCalculator.Calculate(this._serviceRequest);

            //Assert
            result.Should().Be(expectedResult);
        }
    }
}

using AutoFixture;
using FluentAssertions;
using IPO.Common.ServiceRequest.Models;
using IPO.Common.ServiceRequest.Models.PatentApplyAndSearch;
using IPO.FeeService.Services.Calculating.Calculators;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IPO.FeeService.UnitTests.Services
{
    [TestClass]
    public class PatentApplyAndSearchCalculatorTests
    {
        private readonly Fixture _fixture;
        private readonly PatentApplyAndSearchCalculator _PatentApplyAndSearchCalculator;
        private ServiceRequest _serviceRequest;
        public PatentApplyAndSearchCalculatorTests()
        {
            this._fixture = new Fixture();
            this._PatentApplyAndSearchCalculator = new PatentApplyAndSearchCalculator();
            this._serviceRequest = this._fixture.Build<ServiceRequest>()
                                    .With(o => o.Header,
                                    this._fixture.Build<RequestHeader>().With(o => o.Type,
                                    ServiceRequestType.PatentApplyAndSearch).Create())
                                    .With(o => o.Details, this._fixture.Build<PatentApplyAndSearchRequest>()
                                    .With(o => o.NumberOfClaims, 30)
                                    .Create())
                                    .Create();
        }
        [TestMethod]
        public void CalculatorContainsRightRequestType()
        {
            // Arrange    
            var expectedType = ServiceRequestType.PatentApplyAndSearch;

            // Act   
            var result = this._PatentApplyAndSearchCalculator.RequestType;

            // Assert  
            result.Should().Be(expectedType);
        }

        [TestMethod]
        public void CalculatorCalculateWhenTypeDigitalReturnsRightAmount()
        {
            //Arrange
            var expectedResult = 31000;
            this._serviceRequest.Header.Submission = SubmissionType.Digital;
            //Act
            var result = this._PatentApplyAndSearchCalculator.Calculate(this._serviceRequest);

            //Assert
            result.Should().Be(expectedResult);
        }
        [TestMethod]
        public void CalculatorCalculateWhenTypePaperReturnsRightAmount()
        {
            //Arrange
            var expectedResult = 37000;
            this._serviceRequest.Header.Submission = SubmissionType.Paper;
            //Act
            var result = this._PatentApplyAndSearchCalculator.Calculate(this._serviceRequest);

            //Assert
            result.Should().Be(expectedResult);
        }

        [TestMethod]
        public void CalculatorCalculateWhenRightTypeIsNotPatentReturnsRightAmount()
        {
            //Arrange
            var expectedResult = 0;
            ((PatentApplyAndSearchRequest)this._serviceRequest.Details).RightType = RightType.Designs;
            //Act
            var result = this._PatentApplyAndSearchCalculator.Calculate(this._serviceRequest);

            //Assert
            result.Should().Be(expectedResult);
        }
    }
}

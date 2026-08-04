using AutoFixture;
using FluentAssertions;
using IPO.Common.ServiceRequest.Models;
using IPO.Common.ServiceRequest.Models.PatentApplySearchAndExam;
using IPO.FeeService.Services.Calculating.Calculators;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IPO.FeeService.UnitTests.Services
{
    [TestClass]
    public class PatentApplySearchAndExamCalculatorTests
    {
        private readonly Fixture _fixture;
        private readonly PatentApplySearchAndExamCalculator _PatentApplySearchAndExamCalculator;
        private readonly ServiceRequest _serviceRequest;
        public PatentApplySearchAndExamCalculatorTests()
        {
            this._fixture = new Fixture();
            this._PatentApplySearchAndExamCalculator = new PatentApplySearchAndExamCalculator();
            this._serviceRequest = this._fixture.Build<ServiceRequest>()
                                    .With(o => o.Header,
                                    this._fixture.Build<RequestHeader>().With(o => o.Type,
                                    ServiceRequestType.PatentApplySearchAndExam).Create())
                                    .With(o => o.Details, this._fixture.Build<PatentApplySearchAndExamRequest>()
                                    .With(o => o.NumberOfPages, 50)
                                    .With(o => o.NumberOfClaims, 50)
                                    .Create())
                                    .Create();
        }
        [TestMethod]
        public void CalculatorContainsRightRequestType()
        {
            // Arrange    
            var expectedType = ServiceRequestType.PatentApplySearchAndExam;

            // Act   
            var result = this._PatentApplySearchAndExamCalculator.RequestType;

            // Assert  
            result.Should().Be(expectedType);
        }

        [TestMethod]
        public void CalculatorCalculateWhenTypeDigitaReturnsRightAmount()
        {
            //Arrange
            var expectedResult = 96000;
            this._serviceRequest.Header.Submission = SubmissionType.Digital;

            //Act
            var result = this._PatentApplySearchAndExamCalculator.Calculate(this._serviceRequest);

            //Assert
            result.Should().Be(expectedResult);
        }

        [TestMethod]
        public void CalculatorCalculateWhenTypePaperReturnsRightAmount()
        {
            //Arrange
            var expectedResult = 105000;
            this._serviceRequest.Header.Submission = SubmissionType.Paper;

            //Act
            var result = this._PatentApplySearchAndExamCalculator.Calculate(this._serviceRequest);

            //Assert
            result.Should().Be(expectedResult);
        }


        [TestMethod]
        public void CalculatorCalculateWhenRightTypeIsNotPatentReturnsRightAmount()
        {
            //Arrange
            var expectedResult = 0;
            ((PatentApplySearchAndExamRequest)this._serviceRequest.Details).RightType = RightType.Designs;
            //Act
            var result = this._PatentApplySearchAndExamCalculator.Calculate(this._serviceRequest);

            //Assert
            result.Should().Be(expectedResult);
        }
    }
}

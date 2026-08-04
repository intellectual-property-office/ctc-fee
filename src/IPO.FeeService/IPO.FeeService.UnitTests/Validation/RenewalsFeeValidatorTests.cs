using AwesomeAssertions;
using IPO.FeeService.Models.Validation;
using IPO.FeeService.Services.Services;
using IPO.FeeService.Services.Validation;
using Microsoft.AspNetCore.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace IPO.FeeService.UnitTests.Validation
{
    [TestClass]
    public class RenewalsFeeValidatorTests
    {
        private readonly IRenewalsFeeValidator _renewalsFeeValidator;
        private const string RightIdError = "The request details could not be processed. The following mandatory field is missing: rightId";
        private const string RightTypeError = "The request details could not be processed. The following mandatory field is missing: rightType";
        private const string PatentTypeError = "The request details could not be processed. The following mandatory field is missing: patentType";
        private const string FromRenewalYearError = "The request details could not be processed. The following mandatory field is missing: fromRenewalYear";
        private const string RenewalYearError = "The request details could not be processed. The following mandatory field is missing: renewalYear";
        private const string IsLateGrantError = "The request details could not be processed. The following mandatory field is missing: isLateGrant";
        private const string IsLORError = "The request details could not be processed. The following mandatory field is missing: isLOR";

        public RenewalsFeeValidatorTests()
        {
            _renewalsFeeValidator = new RenewalsFeeValidator();
        }

        [TestMethod]
        [DataRow("rightId")]
        [DataRow("rightType")]
        [DataRow("patentType")]
        [DataRow("fromRenewalYear")]
        [DataRow("renewalYear")]
        [DataRow("isLateGrant")]
        [DataRow("isLOR")]
        public void ValidateRequestWithNullRequiredRenewalValuesGivesErrors(string fieldName)
        {
            //Arramge
            var requestDetails = Helper.BuildFeeCalculationRequestDetailsWithMissingField(fieldName);

            //Act
           var validationResult = _renewalsFeeValidator.ValidateRequest(requestDetails);

            //Assert
            validationResult.Should().NotBeNull();
            validationResult.FirstOrDefault()!.Code.Should().Be(422);
			var error = validationResult.FirstOrDefault();
			Assert.IsTrue(
				error?.ErrorMessage != null &&
				error.ErrorMessage.Contains(@"The request details could not be processed. The following mandatory field is missing: " + fieldName),
				"Expected error message for missing mandatory renewal field was not found."
			);
		}

		[TestMethod]
		[DataRow("isRestoration")]
		public void ValidateRequestWithMissingOptionalRenewalValuesIsSuccess(string fieldName)
		{
			//Arramge
			var requestDetails = Helper.BuildFeeCalculationRequestDetailsWithMissingField(fieldName);

			//Act
			List<RenewalsFeeValidationResult> validationResult = _renewalsFeeValidator.ValidateRequest(requestDetails);

			//Assert
			validationResult.Should().NotBeNull();
            validationResult.Count.Should().Be(0);
		}

		[TestMethod]
        public void GetStatusCodeExceptionListReturnsListOfErrorsWithCode()
        {
            //Arrange
            var validationResults = GetFeeCalculationValidationResults();
            var expectedCode = 422;
            var expectedErrorCode = "E006";

            //Act
            var exceptionList = RenewalsFeeValidator.GetStatusCodeExceptionList<RenewalsFeeValidator>(expectedCode, expectedErrorCode, validationResults);

            //Assert
            exceptionList.StatusCode.Should().Be(StatusCodes.Status422UnprocessableEntity);
            exceptionList.Errors.Should().NotBeEmpty();
            exceptionList.Errors.Count.Should().Be(7);
            exceptionList.Errors.All(a => a.Code == "E005");
            exceptionList.Errors[0].Description = RightIdError;
            exceptionList.Errors[1].Description = RightTypeError;
            exceptionList.Errors[2].Description = PatentTypeError;
            exceptionList.Errors[3].Description = FromRenewalYearError;
            exceptionList.Errors[4].Description = RenewalYearError;
            exceptionList.Errors[5].Description = IsLateGrantError;
            exceptionList.Errors[6].Description = IsLORError;
        }

        private List<RenewalsFeeValidationResult> GetFeeCalculationValidationResults()
        {
            return new List<RenewalsFeeValidationResult>()
            {
                new RenewalsFeeValidationResult()
                {
                    Code = 422,
                    ErrorMessage = RightIdError
                },
                new RenewalsFeeValidationResult()
                {
                    Code = 422,
                    ErrorMessage = RightTypeError
                },
                new RenewalsFeeValidationResult()
                {
                    Code = 422,
                    ErrorMessage = PatentTypeError
                },
                new RenewalsFeeValidationResult()
                {
                    Code = 422,
                    ErrorMessage = FromRenewalYearError
                },
                new RenewalsFeeValidationResult()
                {
                    Code = 422,
                    ErrorMessage = RenewalYearError
                },
                new RenewalsFeeValidationResult()
                {
                    Code = 422,
                    ErrorMessage = IsLateGrantError
                },
                new RenewalsFeeValidationResult()
                {
                    Code = 422,
                    ErrorMessage = IsLORError
                }
            };
        }
    }
}

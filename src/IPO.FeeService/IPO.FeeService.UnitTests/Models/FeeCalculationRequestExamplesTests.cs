using AwesomeAssertions;
using IPO.FeeService.API;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Swashbuckle.AspNetCore.Swagger;
using System.Linq;

namespace IPO.FeeService.UnitTests.Models
{
    [TestClass]
    public class FeeCalculationRequestExamplesTests
    {
        private OpenApiDocument _swagger = new OpenApiDocument();
        private IHost _server;

        public FeeCalculationRequestExamplesTests()
        {
            _server = Host.CreateDefaultBuilder()
            .ConfigureWebHostDefaults(options => { options.UseStartup<Startup>(); })
            .Build();

            _swagger = _server.Services
            .GetRequiredService<ISwaggerProvider>()
            .GetSwagger("v1"); //the name of your API

        }

        [TestMethod]
        [DataRow("Acceleration")]
        [DataRow("ApplicationToRestoreAPatent")]
        [DataRow("ApplicationForDeclarationOfLapseOrInvalidityOrToRevokeAnExtensionOfTheDurationOfAnSPC")]
        [DataRow("ApplicationToBeMadeAPartyToProceedings")]
        [DataRow("ApplyToSettleOrAdjustLicenseOfRightTermsPreAugust1989")]
        [DataRow("ContinuationOfProceedingsBeforeTheComptroller")]
        [DataRow("CertifiedOfficeCopies")]
        [DataRow("CancellationOfLicenseOfRight")]
        [DataRow("ChangeLicenceInterest")]
        [DataRow("ChangeOwnerOrGiveNoticeOfRights")]
        [DataRow("ChangeSecurityInterest")]
        [DataRow("ChangeOfRepresentative")]
        [DataRow("ExtensionOfTime")]
        [DataRow("GrantFee")]
        [DataRow("InitiationOfProceedingsBeforeTheComptroller")]
        [DataRow("InternationalSearch")]
        [DataRow("LateClaimToPriority")]
        [DataRow("LicenceOfRight")]
        [DataRow("NoticeOfOppositionToProceedingsBeforeTheComptroller")]
        [DataRow("PatentApplication")]
        [DataRow("PatentApplicationAndSearch")]
        [DataRow("PatentApplicationSearchAndExam")]
        [DataRow("PatentExam")]
        [DataRow("PatentFurtherSearch")]
        [DataRow("PatentSearch")]
        [DataRow("PatentSearchAndExam")]
        [DataRow("PatentSupplementarySearch")]
        [DataRow("PaymentOfAnnualSpcFees")]
        [DataRow("PctAdditionalPage")]
        [DataRow("PcteFilingReductionFull")]
        [DataRow("PcteFilingReductionPartial")]
        [DataRow("PctInternationalFiling")]
        [DataRow("PctPriorityDocument")]
        [DataRow("PctRestorationOfPriority")]
        [DataRow("PctSearchFee")]
        [DataRow("PctTransmittalFee")]
        [DataRow("PostGrantAmendment")]
        [DataRow("PublicationOfTranslation")]
        [DataRow("ReferenceApplication")]
        [DataRow("ReferenceOfDisputeToComptroller")]
        [DataRow("Reinstatement")]
        [DataRow("Renewals")]
        [DataRow("RequestForOpinionAsToValidityOrInfringementOfAPatent")]
        [DataRow("Np1Application")]
        [DataRow("Np1ApplicationAndSearch")]
        [DataRow("Np1ApplicationSearchAndExam")]
        [DataRow("SpcApplication")]
        [DataRow("SpcDeclarationOfInvalidity")]
        [DataRow("SpcExtension")]
        [DataRow("SurrenderARight")]
        [DataRow("TransferOfOwner")]
        [DataRow("UncertifiedOfficeCopies")]
        [DataRow("UpdateAddressOnARight")]
        [DataRow("UpdateNameOnARight")]
        [DataRow("VaryLicenceOfRightTermsByDesignRightOrCopyrightOwner")]
        public void GetExamplesReturnsExampleRequests(string serviceRequestType)
        {
            //Act & Assert

            _swagger.Should().NotBeNull();
            _swagger.Components.Schemas.Should().NotBeNull();
            _swagger.Paths["/calculate/{serviceRequestType}"].Operations.First().Value.RequestBody
                .Content.First().Value.Examples.Keys.Should().Contain(serviceRequestType);

        }
    }
}

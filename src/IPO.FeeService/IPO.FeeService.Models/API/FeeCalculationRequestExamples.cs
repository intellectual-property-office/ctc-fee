using Swashbuckle.AspNetCore.Filters;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace IPO.FeeService.Models.API
{
    [ExcludeFromCodeCoverage]
    public class FeeCalculationRequestExamples : IMultipleExamplesProvider<JsonDocument>
    {
        public IEnumerable<SwaggerExample<JsonDocument>> GetExamples()
        {
            yield return SwaggerExample.Create("Acceleration", GetDateOnlyRequest());
            yield return SwaggerExample.Create("ApplicationForDeclarationOfLapseOrInvalidityOrToRevokeAnExtensionOfTheDurationOfAnSPC", GetDateOnlyRequest());
            yield return SwaggerExample.Create("ApplicationToBeMadeAPartyToProceedings", GetDateOnlyRequest());
            yield return SwaggerExample.Create("ApplicationToRestoreAPatent", GetDateOnlyRequest());
            yield return SwaggerExample.Create("ApplyToSettleOrAdjustLicenseOfRightTermsPreAugust1989", GetDateOnlyRequest());
            yield return SwaggerExample.Create("CertifiedOfficeCopies", GetCertifiedOfficeCopiesRequest());
            yield return SwaggerExample.Create("ContinuationOfProceedingsBeforeTheComptroller", GetDateOnlyRequest());
            yield return SwaggerExample.Create("CancellationOfLicenseOfRight", GetDateOnlyRequest());
            yield return SwaggerExample.Create("ChangeLicenceInterest", GetManageNumberOfPatentsRequest());
            yield return SwaggerExample.Create("ChangeOwnerOrGiveNoticeOfRights", GetManageNumberOfPatentsRequest());
            yield return SwaggerExample.Create("ChangeSecurityInterest", GetManageNumberOfPatentsRequest());
            yield return SwaggerExample.Create("ChangeOfRepresentative", GetNumberOfChangesRequest());
            yield return SwaggerExample.Create("ExtensionOfTime", GetExtensionOfTimeRequest());
            yield return SwaggerExample.Create("GrantFee", GetClaimsAndPagesRequest());
            yield return SwaggerExample.Create("InitiationOfProceedingsBeforeTheComptroller", GetDateOnlyRequest());
            yield return SwaggerExample.Create("InternationalSearch", GetDateOnlyRequest());
            yield return SwaggerExample.Create("LateClaimToPriority", GetLateClaimToPriority());
            yield return SwaggerExample.Create("LicenceOfRight", GetDateOnlyRequest());
            yield return SwaggerExample.Create("NoticeOfOppositionToProceedingsBeforeTheComptroller", GetDateOnlyRequest());
            yield return SwaggerExample.Create("PatentApplication", GetLateDeclarationRequest());
            yield return SwaggerExample.Create("PatentApplicationAndSearch", GetLateDeclarationClaimsRequest());
            yield return SwaggerExample.Create("PatentApplicationSearchAndExam", GetLateDeclarationClaimsAndPagesRequest());
            yield return SwaggerExample.Create("PatentExam", GetPagesRequest());
            yield return SwaggerExample.Create("PatentFurtherSearch", GetDateOnlyRequest());
            yield return SwaggerExample.Create("PatentSearch", GetPatentSearchWithClaimsCorrectionRequest());
            yield return SwaggerExample.Create("PatentSearchAndExam", GetClaimsAndPagesRequest());
            yield return SwaggerExample.Create("PatentSupplementarySearch", GetDateOnlyRequest());
            yield return SwaggerExample.Create("PaymentOfAnnualSpcFees", GetPaymentOfAnnualSpcFeesExampleRequest());
            yield return SwaggerExample.Create("PctAdditionalPage", GetPagesRequest());
            yield return SwaggerExample.Create("PcteFilingReductionFull", GetDateOnlyRequest());
            yield return SwaggerExample.Create("PcteFilingReductionPartial", GetDateOnlyRequest());
            yield return SwaggerExample.Create("PctInternationalFiling", GetDateOnlyRequest());
            yield return SwaggerExample.Create("PctPriorityDocument", GetDateOnlyRequest());
            yield return SwaggerExample.Create("PctRestorationOfPriority", GetDateOnlyRequest());
            yield return SwaggerExample.Create("PctSearchFee", GetDateOnlyRequest());
            yield return SwaggerExample.Create("PctTransmittalFee", GetDateOnlyRequest());
            yield return SwaggerExample.Create("PostGrantAmendment", GetDateOnlyRequest());
            yield return SwaggerExample.Create("PublicationOfTranslation", GetPublishTranslationRequest());
            yield return SwaggerExample.Create("ReferenceApplication", GetDateOnlyRequest());
            yield return SwaggerExample.Create("ReferenceOfDisputeToComptroller", GetDateOnlyRequest());
            yield return SwaggerExample.Create("Reinstatement", GetDateOnlyRequest());
            yield return SwaggerExample.Create("Renewals", GetRenewalsRequest());
            yield return SwaggerExample.Create("RequestForOpinionAsToValidityOrInfringementOfAPatent", GetDateOnlyRequest());
            yield return SwaggerExample.Create("Np1Application", GetDateOnlyRequest());
            yield return SwaggerExample.Create("Np1ApplicationAndSearch", GetNp1ApplicationAndSearchRequest());
            yield return SwaggerExample.Create("Np1ApplicationSearchAndExam", GetNp1ApplicationSearchAndExamRequest());
            yield return SwaggerExample.Create("SpcApplication", GetDateOnlyRequest());
            yield return SwaggerExample.Create("SpcDeclarationOfInvalidity", GetDateOnlyRequest());
            yield return SwaggerExample.Create("SpcExtension", GetDateOnlyRequest());
            yield return SwaggerExample.Create("SurrenderARight", GetDateOnlyRequest());
            yield return SwaggerExample.Create("TransferOfOwner", GetTransferOfOwnerRequest());
            yield return SwaggerExample.Create("UpdateAddressOnARight", GetNumberOfChangesRequest());
            yield return SwaggerExample.Create("UpdateNameOnARight", GetNumberOfChangesRequest());
            yield return SwaggerExample.Create("UncertifiedOfficeCopies", GetDateOnlyRequest());
            yield return SwaggerExample.Create("VaryLicenceOfRightTermsByDesignRightOrCopyrightOwner", GetDateOnlyRequest());
        }

        private static JsonDocument GetDateOnlyRequest()
        {
            return JsonDocument.Parse(@"{
                          ""paidDate"": ""2023-05-13""
                          }");
        }

        private static JsonDocument GetPatentSearchWithClaimsCorrectionRequest()
        {
            return JsonDocument.Parse(@"{
                      ""paidDate"": ""2023-05-13"",
                      ""requestDetails"": [ {
                            ""numberOfClaims"": 15,
                            ""numberOfClaimsCorrection"": 0,
                            ""isApplicationFeePaid"": false
                            }]
                          }");
        }

        private static JsonDocument GetCertifiedOfficeCopiesRequest()
        {
            return JsonDocument.Parse(@"{
                          ""paidDate"": ""2023-05-13"",
                          ""requestDetails"": [ {
                                ""numberOfCertifiedCopies"": 1
                                }]
                          }");
        }

        private static JsonDocument GetTransferOfOwnerRequest()
        {
            return JsonDocument.Parse(@"{
                          ""paidDate"": ""2023-05-13"",
                          ""requestDetails"": [ {
                                ""numberOfPatents"": 30,
                                ""numberOfTrademarks"": 37,
                                ""numberOfDesigns"": 8
                                }]
                          }");
        }

        private static JsonDocument GetClaimsAndPagesRequest()
        {
            return JsonDocument.Parse(@"{
                          ""paidDate"": ""2023-05-13"",
                          ""requestDetails"": [ {
                                ""numberOfClaims"": 15,
                                ""numberOfPages"": 32,
                                ""numberOfClaimsCorrection"": 0,
                                ""numberOfPagesCorrection"": 0,
                                ""isApplicationFeePaid"": false
                                }]
                          }");
        }

        private static JsonDocument GetLateDeclarationClaimsAndPagesRequest()
        {
            return JsonDocument.Parse(@"{
                          ""paidDate"": ""2023-05-13"",
                          ""requestDetails"": [ {
                                ""isLateDeclarationOfPriority"": true,
                                ""numberOfClaims"": 15,
                                ""numberOfPages"": 32,
                                ""numberOfClaimsCorrection"": 0,
                                ""numberOfPagesCorrection"": 0
                                }]
                          }");
        }

        private static JsonDocument GetPagesRequest()
        {
            return JsonDocument.Parse(@"{
                          ""paidDate"": ""2023-05-13"",
                          ""requestDetails"": [ {
                                ""numberOfPages"": 32,
                                ""numberOfPagesCorrection"": 0
                                }]
                          }");
        }

        private static JsonDocument GetRenewalsRequest()
        {
            return JsonDocument.Parse(@"{
                          ""paidDate"": ""2023-05-13"",
                          ""requestDetails"": [ {
                                ""rightId"": 1234567891,
                                ""rightType"": ""Patent"",
                                ""patentType"": ""EP"",
                                ""fromRenewalYear"": 5,
                                ""renewalYear"": 5,
                                ""isLateGrant"": false,
                                ""isLOR"": false,
                                ""renewalDueDate"": ""2023-04-16"",
                                ""paymentDate"": ""2023-05-14"",
                                ""isRestoration"": false
                                }]
                          }");
        }

        private static JsonDocument GetManageNumberOfPatentsRequest()
        {
            return JsonDocument.Parse(@"{
                    ""paidDate"": ""2023-05-13"",
                    ""requestDetails"": [ {
                          ""numberOfPatents"": ""24""
                          }]
            }");
        }

        private static JsonDocument GetNumberOfChangesRequest()
        {
            return JsonDocument.Parse(@"{
                        ""paidDate"": ""2023-05-13"",
                        ""requestDetails"": [ {
                            ""numberOfChanges"": 15
                            }]
                        }");
        }

        private static JsonDocument GetExtensionOfTimeRequest()
        {
            return JsonDocument.Parse(@"{
                          ""paidDate"": ""2023-05-13"",
                          ""requestDetails"": [ {
                                ""isSpecified"": false,
                                ""isFoc"": false
                                }]
                          }");
        }

        private static JsonDocument GetLateDeclarationRequest()
        {
            return JsonDocument.Parse(@"{
                    ""paidDate"": ""2023-05-13"",
                    ""requestDetails"": [ {
                           ""isLateDeclarationOfPriority"": true
                          }]
            }");
        }

        private static JsonDocument GetLateDeclarationClaimsRequest()
        {
            return JsonDocument.Parse(@"{
                    ""paidDate"": ""2023-05-13"",
                    ""requestDetails"": [ {
                           ""isLateDeclarationOfPriority"": true,
                           ""numberOfClaims"": 37,
                           ""numberOfClaimsCorrection"": 0
                          }]
            }");
        }

        private static JsonDocument GetPublishTranslationRequest()
        {
            return JsonDocument.Parse(@"{
                    ""paidDate"": ""2023-05-13"",
                    ""requestDetails"": [ {
                          ""isPublishTranslation"": true
                          }]
            }");
        }

        private static JsonDocument GetNp1ApplicationAndSearchRequest()
        {
            return JsonDocument.Parse(@"{
                    ""paidDate"": ""2023-05-13"",
                    ""requestDetails"": [ {
                        ""isPublishTranslation"": true,
                        ""isLateDeclarationOfPriority"": true,
                        ""numberOfClaims"": 37,
                        ""numberOfClaimsCorrection"": 0
                          }]
            }");
        }

        private static JsonDocument GetNp1ApplicationSearchAndExamRequest()
        {
            return JsonDocument.Parse(@"{
                    ""paidDate"": ""2023-05-13"",
                    ""requestDetails"": [ {
                        ""isPublishTranslation"": true,
                        ""isLateDeclarationOfPriority"": true,
                        ""numberOfClaims"": 37,
                        ""numberOfPages"": 42,
                        ""numberOfClaimsCorrection"": 0,
                        ""numberOfPagesCorrection"": 0
                          }]
            }");
        }

        private static JsonDocument GetPaymentOfAnnualSpcFeesExampleRequest()
        {
            return JsonDocument.Parse(@"{
                    ""paidDate"": ""2023-05-13"",
                    ""requestDetails"": [ {
                        ""spcYear"": 5,
                        ""isLate"": true
                          }]
            }");
        }

        private static JsonDocument GetLateClaimToPriority()
        {
            return JsonDocument.Parse(@"{
                          ""paidDate"": ""2023-05-13"",
                          ""requestDetails"": [ {
                                ""isLateDeclarationOfPriority"": false
                                }]
                          }");
        }
    }
}

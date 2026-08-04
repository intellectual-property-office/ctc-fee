using IPO.FeeService.Data;
using IPO.FeeService.Models.API;
using IPO.FeeService.Models.Constants;
using IPO.FeeService.Models.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using ServiceRequestType = IPO.FeeService.Models.Data.ServiceRequestType;

namespace IPO.FeeService.UnitTests
{
    [ExcludeFromCodeCoverage]
    public static class Helper
    {
        [ExcludeFromCodeCoverage]
        public static int Minimise(int? n) => n > 0 ? 1 : 0;

        [ExcludeFromCodeCoverage]
        public static int TransferOfOwnerQuantityCount(FeeCalculationRequest request) =>
            Minimise(request.RequestDetails!.FirstOrDefault()!.NumberOfTrademarks) +
            Minimise(request.RequestDetails!.FirstOrDefault()!.NumberOfPatents) +
            Minimise(request.RequestDetails!.FirstOrDefault()!.NumberOfDesigns);

        [ExcludeFromCodeCoverage]
        public static async Task SetupAllServiceRequestDatasetsInRepo(FeeDbRepository feeDbRepository, string channel, decimal price)
        {
            int index = 1;

            foreach (int serviceRequestType in Enum.GetValues(typeof(ServiceRequestTypeEnum)))
            {
                string serviceRequestTypeString = Enum.GetName(typeof(ServiceRequestTypeEnum), serviceRequestType)!;
                string productCode = $"TEST-{index}";
                string productDescription = $"Test Product No. {index}";
                var serviceRequestRecord = CreateFullServiceRequestDataSet(index, serviceRequestTypeString!, channel, productCode, productDescription, price);
                feeDbRepository.Context.ServiceRequestTypes!.Add(serviceRequestRecord);
				var serviceRequestRecordProducts = serviceRequestRecord.ProductServiceRequestTypes.Select(x => x.Product);

				foreach (var product in serviceRequestRecordProducts)
                {
                    feeDbRepository.Context.Products!.Add(product!);

                    foreach (var fee in product!.Fees!)
                    {
                        feeDbRepository.Context.Fees!.Add(fee);
                    }

                    foreach (var quantityRule in product.QuantityRules)
                    {
                        feeDbRepository.Context.QuantityRules!.Add(quantityRule);
                    }

                    foreach (var taxCode in product.TaxCodes)
                    {
                        feeDbRepository.Context.TaxCodes!.Add(taxCode);
                    }
                }

                index++;
            }

            await feeDbRepository.Context.SaveChangesAsync();
        }

        [ExcludeFromCodeCoverage]
        public static async Task SetupIndividualServiceRequestDatasetInRepo(FeeDbRepository feeDbRepository, int id, string serviceRequestTypeString, string channel, decimal price)
        {
            string productCode = $"TEST-{id}";
            string productDescription = $"Test Product No. {id}";
            var serviceRequestRecord = CreateFullServiceRequestDataSet(id, serviceRequestTypeString, channel, productCode, productDescription, price);
            feeDbRepository.Context.ServiceRequestTypes!.Add(serviceRequestRecord);
			var serviceRequestRecordProducts = serviceRequestRecord.ProductServiceRequestTypes.Select(x => x.Product);

			foreach (var product in serviceRequestRecordProducts)
            {
                feeDbRepository.Context.Products!.Add(product!);

                foreach (var fee in product!.Fees!)
                {
                    feeDbRepository.Context.Fees!.Add(fee);
                }

                foreach (var quantityRule in product.QuantityRules)
                {
                    feeDbRepository.Context.QuantityRules!.Add(quantityRule);
                }

                foreach (var taxCode in product.TaxCodes)
                {
                    feeDbRepository.Context.TaxCodes!.Add(taxCode);
                }
            }

            await feeDbRepository.Context.SaveChangesAsync();
        }

        [ExcludeFromCodeCoverage]
        public static int SetupServiceRequestTypesInRepo(FeeDbRepository feeDbRepository)
        {
            int index = 1;
            foreach (int serviceRequestType in Enum.GetValues(typeof(ServiceRequestTypeEnum)))
            {
                string serviceRequestTypeString = Enum.GetName(typeof(ServiceRequestTypeEnum), serviceRequestType)!;
                var serviceRequestRecord = CreateServiceRequestType(index, serviceRequestTypeString!);
                feeDbRepository.Context.ServiceRequestTypes!.Add(serviceRequestRecord);
                index++;
            }

            return feeDbRepository.Context.SaveChanges();
        }

        [ExcludeFromCodeCoverage]
        public static async Task CreateServiceRequestTypeInRepo(FeeDbRepository feeDbRepository, int id, string serviceRequestTypeString, string e5AccountNumber = "")
        {
            var serviceRequestRecord = CreateServiceRequestType(id, serviceRequestTypeString, e5AccountNumber);
            feeDbRepository.Context.ServiceRequestTypes!.Add(serviceRequestRecord);
            await feeDbRepository.Context.SaveChangesAsync();
        }

        [ExcludeFromCodeCoverage]
        public static async Task AddCompleteProductToServiceRequestType(FeeDbRepository feeDbRepository, string serviceRequestTypeString, string channel, string productCode, string productDescription, decimal taxRate, string taxRateCode, decimal price, string rule, DateTime referenceDate, int productSequence = 1)
        {
            ServiceRequestType serviceRequestRecord = feeDbRepository.Context.ServiceRequestTypes!.First(x => x.Name == serviceRequestTypeString);

            DateTime effectiveFrom = referenceDate.AddYears(-1);
            DateTime effectiveTo = referenceDate.AddYears(2);

            int taxId = feeDbRepository.Context.TaxCodes!.Any() ? feeDbRepository.Context.TaxCodes!.Last().Id + 1 : 1;
            TaxCode taxCode = CreateTaxCode(taxId, taxRateCode, taxRate, effectiveFrom, effectiveTo);

            int quantityRuleId = feeDbRepository.Context.QuantityRules!.Any() ? feeDbRepository.Context.QuantityRules!.Last().Id + 1 : 1;
            QuantityRule quantityRule = CreateQuantityRule(quantityRuleId, rule, effectiveFrom, effectiveTo);

            int feeId = feeDbRepository.Context.Fees!.Any() ? feeDbRepository.Context.Fees!.Last().Id + 1 : 1;
            Fee fee = CreateFee(feeId, price, effectiveFrom, effectiveTo);

            int productId = feeDbRepository.Context.Products!.Any() ? feeDbRepository.Context.Products!.Last().Id + 1 : 1;
            Product product = CreateProduct(productId, channel, productCode, productDescription, taxCode, quantityRule, new Collection<Fee> { fee });
			ProductServiceRequestType productServiceRequestType = CreateProductServiceRequestType(product, serviceRequestRecord, productSequence);

            feeDbRepository.Context.Products!.Add(product);

            foreach (var modifiedFee in product.Fees!)
            {
                feeDbRepository.Context.Fees!.Add(modifiedFee);
            }

            foreach (var modifiedQuantityRule in product.QuantityRules)
            {
                feeDbRepository.Context.QuantityRules!.Add(modifiedQuantityRule);
            }

            foreach (var modifiedTaxCode in product.TaxCodes)
            {
                feeDbRepository.Context.TaxCodes!.Add(modifiedTaxCode);
            }

            await feeDbRepository.Context.SaveChangesAsync();
        }

        [ExcludeFromCodeCoverage]
        public static async Task AddCompleteProductToServiceRequestTypeMultipleFees(FeeDbRepository feeDbRepository, string serviceRequestTypeString, string channel, string productCode, string productDescription, decimal taxRate, string taxRateCode, string rule, List<(DateTime EffectiveFrom, DateTime? EffectiveTo, decimal PriceExVat)> feeDetails, int productNumber, int productSequence = 1)
        {
            ServiceRequestType serviceRequestRecord = feeDbRepository.Context.ServiceRequestTypes!.First(x => x.Name == serviceRequestTypeString);


            int taxId = feeDbRepository.Context.TaxCodes!.Any() ? feeDbRepository.Context.TaxCodes!.Last().Id + 1 : 1;
            TaxCode taxCode = CreateTaxCode(taxId, taxRateCode, taxRate, feeDetails.First().EffectiveFrom, null);

            int quantityRuleId = feeDbRepository.Context.QuantityRules!.Any() ? feeDbRepository.Context.QuantityRules!.Last().Id + 1 : 1;
            QuantityRule quantityRule = CreateQuantityRule(quantityRuleId, rule, feeDetails.First().EffectiveFrom, null);


            var fees = new Collection<Fee>();
            int feeId = feeDbRepository.Context.Fees!.Any() ? feeDbRepository.Context.Fees!.Max(f => f.Id) + 1 : 1;
            foreach (var feeDetail in feeDetails)
            {
                Fee fee = CreateFee(feeId, feeDetail.PriceExVat, feeDetail.EffectiveFrom, feeDetail.EffectiveTo);
                fees.Add(fee);
                feeId++;
            }
            

            int productId = feeDbRepository.Context.Products!.Any() ? feeDbRepository.Context.Products!.Last().Id + 1 : 1;
            Product product = CreateProduct(productId, channel, productCode, productDescription, taxCode, quantityRule, fees, productNumber);
            ProductServiceRequestType productServiceRequestType = CreateProductServiceRequestType(product, serviceRequestRecord, productSequence);

            feeDbRepository.Context.Products!.Add(product);

            foreach (var modifiedFee in product.Fees!)
            {
                feeDbRepository.Context.Fees!.Add(modifiedFee);
            }

            foreach (var modifiedQuantityRule in product.QuantityRules)
            {
                feeDbRepository.Context.QuantityRules!.Add(modifiedQuantityRule);
            }

            foreach (var modifiedTaxCode in product.TaxCodes)
            {
                feeDbRepository.Context.TaxCodes!.Add(modifiedTaxCode);
            }

            await feeDbRepository.Context.SaveChangesAsync();
        }

        [ExcludeFromCodeCoverage]
        private static ServiceRequestType CreateFullServiceRequestDataSet(int id, string serviceRequestName, string channel, string productCode, string productDescription, decimal price)
		{
			ServiceRequestType serviceRequestType = CreateServiceRequestType(id, serviceRequestName);
			
			DateTime effectiveFrom = DateTime.Now.AddYears(-1);
			DateTime effectiveTo = DateTime.Now.AddYears(1);
			string code = "OS";
			decimal rate = 0.00M;
			string rule = Settings.NoQuantityRuleValue;

			TaxCode taxCode = CreateTaxCode(id, code, rate, effectiveFrom, effectiveTo);
			QuantityRule quantityRule = CreateQuantityRule(id, rule, effectiveFrom, effectiveTo);
			Fee fee = CreateFee(id, price, effectiveFrom, effectiveTo);

			Product product = CreateProduct(id, channel, productCode, productDescription, taxCode, quantityRule, new Collection<Fee> { fee });
            ProductServiceRequestType productServiceRequestType = CreateProductServiceRequestType(product, serviceRequestType);

			return serviceRequestType;
		}

		[ExcludeFromCodeCoverage]
        private static ServiceRequestType CreateServiceRequestType(int index, string srName, string e5AccountNumber = "")
        {
            return new ServiceRequestType()
            {
                Id = index,
                Name = srName,
                E5AccountNumber = e5AccountNumber == "" ? Guid.NewGuid().ToString() : e5AccountNumber
            };
        }
		[ExcludeFromCodeCoverage]
		
        public static ProductServiceRequestType CreateProductServiceRequestType(Product product, ServiceRequestType serviceRequestType, int ProductSequence = 1)
		{
			ProductServiceRequestType productServiceRequestType = new()
			{
				ServiceRequestTypesId = serviceRequestType.Id,
				ProductsId = product.Id,
				ProductSequence = ProductSequence
			};

            if (product != null)
            {
				product.ProductServiceRequestTypes.Add(productServiceRequestType);
				productServiceRequestType.Product = product;
			}

            if (serviceRequestType != null)
            {
				serviceRequestType.ProductServiceRequestTypes.Add(productServiceRequestType);
                productServiceRequestType.ServiceRequestType = serviceRequestType;
			}

            return productServiceRequestType;
		}

        [ExcludeFromCodeCoverage]
        public static Product CreateProduct(int id, string channel, string productCode, string productDescription, TaxCode taxCode, QuantityRule quantityRule, Collection<Fee> fees, int productNumber = 0)
        {
            Product product = new()
            {
                Id = id,
                Channel = channel,
                Code = productCode,
                Description = productDescription,
                ProductNumber = productNumber
            };

            if (taxCode != null)
            {
                taxCode.Products.Add(product);
                product.TaxCodes.Add(taxCode);
            }

            if (quantityRule != null)
            {
                quantityRule.Products.Add(product);
                product.QuantityRules.Add(quantityRule);
            }

            if (fees != null)
            {
                fees.First().Product = product;
                fees.First().ProductId = product.Id;
                product.Fees = fees;

            }

            return product;
        }



        [ExcludeFromCodeCoverage]
        public static TaxCode CreateTaxCode(int id, string code, decimal rate, DateTime effectiveFrom, DateTime? effectiveTo)
        {
            TaxCode taxCode = new()
            {
                Id = id,
                Code = code,
                Rate = rate,
                EffectiveFrom = effectiveFrom,
                EffectiveTo = effectiveTo
            };

            return taxCode;
        }

        [ExcludeFromCodeCoverage]
        public static QuantityRule CreateQuantityRule(int id, string rule, DateTime effectiveFrom, DateTime? effectiveTo)
        {
            QuantityRule quantityRule = new()
            {
                Id = id,
                Rule = rule,
                EffectiveFrom = effectiveFrom,
                EffectiveTo = effectiveTo
            };

            return quantityRule;
        }

        [ExcludeFromCodeCoverage]
        public static Fee CreateFee(int id, decimal price, DateTime effectiveFrom, DateTime? effectiveTo, string feeRule = Settings.NoFeeRuleValue)
        {
            Fee fee = new()
            {
                Id = id,
                PriceExVat = price,
                EffectiveFrom = effectiveFrom,
                EffectiveTo = effectiveTo,
				Rule = feeRule
			};

            return fee;
        }

        [ExcludeFromCodeCoverage]
        public static FeeCalculationRequestDetails BuildFeeCalculationRequestDetailsWithMissingField(string fieldName)
        {
            var feeCalculationRequestDetails = new FeeCalculationRequestDetails();
            var propertyName = ToUpperFirstLetter(fieldName);
            var properties = typeof(FeeCalculationRequestDetails).GetProperties().Select(p => p.Name).ToList();
            properties.Remove(propertyName);

            if (properties.Contains("RightId"))
                feeCalculationRequestDetails.RightId = 5;
            if (properties.Contains("RightType"))
                feeCalculationRequestDetails.RightType = "Patent";
            if (properties.Contains("PatentType"))
                feeCalculationRequestDetails.PatentType = "EP";
            if (properties.Contains("FromRenewalYear"))
                feeCalculationRequestDetails.FromRenewalYear = 5;
            if (properties.Contains("RenewalYear"))
                feeCalculationRequestDetails.RenewalYear = 5;
            if (properties.Contains("IsLateGrant"))
                feeCalculationRequestDetails.IsLateGrant = false;
            if (properties.Contains("IsLOR"))
                feeCalculationRequestDetails.IsLOR = true;
			if (properties.Contains("IsRestoration"))
				feeCalculationRequestDetails.IsRestoration = true;
			if (properties.Contains("RenewalDueDate"))
				feeCalculationRequestDetails.RenewalDueDate = DateTime.Now.ToString("yyyy-MM-dd");
            if (properties.Contains("PaymentDate"))
                feeCalculationRequestDetails.PaymentDate = DateTime.Now.ToString("yyyy-MM-dd");
            return feeCalculationRequestDetails;
        }

        [ExcludeFromCodeCoverage]
        private static string ToUpperFirstLetter(string fieldName)
        {
            char[] letters = fieldName.ToCharArray();

            letters[0] = char.ToUpper(letters[0]);

            return new string(letters);
        }
    }
}

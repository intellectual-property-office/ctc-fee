using System;
using IPO.FeeService.Models.Constants;
using Swashbuckle.AspNetCore.Annotations;
using System.Collections.Generic;
using System.Linq;

namespace IPO.FeeService.Models.API
{
    [SwaggerSchema(Description = "This is the response body for the Calculate endpoint")]
    public class FeeCalculationResults
    {
        public FeeCalculationResults(ServiceRequestTypeEnum serviceRequestType, IEnumerable<FeeItem> breakdown, string accountNumber, string customerChannel)
        {
            ServiceRequestType = serviceRequestType.ToString();
            LineItems = breakdown.ToArray();
            AccountNumber = accountNumber;
            CustomerChannel = customerChannel;
        }

        [SwaggerSchema(Title = "The customer channel for this operation (returned here for reference/validation purposes)")]
        public string CustomerChannel { get; set; }

        [SwaggerSchema(Title = "The account number")]
        public string AccountNumber { get; set; }

        [SwaggerSchema(Title = "The service request type for this operation (returned here for reference/validation purposes)")]
        public string ServiceRequestType { get; set; }

        [SwaggerSchema(Title = "The total fee chargeable excluding VAT")]
        public decimal TotalExVat => LineItems.Sum(x => x.LineTotal) - LineItems.Sum(x => x.VatAmount);

        [SwaggerSchema(Title = "The total VAT chargeable")]
        public decimal TotalVat => LineItems.Sum(x => x.VatAmount);

        [SwaggerSchema(Title = "The total fee chargeable including VAT")]
        public decimal TotalIncVat => LineItems.Sum(x => x.LineTotal);

        [SwaggerSchema(Title = "The breakdown of line items")]
        public FeeItem[] LineItems { get; }
    }
}

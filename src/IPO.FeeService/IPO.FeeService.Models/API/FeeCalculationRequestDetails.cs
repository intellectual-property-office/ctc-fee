using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Swashbuckle.AspNetCore.Annotations;

namespace IPO.FeeService.Models.API
{
    [SwaggerSchema("The details of an individual calculation request", Required = null)]
    public class FeeCalculationRequestDetails
    {
        [SwaggerSchema(Title = "The number of claims", Nullable = true)]
        [Range(0, int.MaxValue)]
        public int? NumberOfClaims { get; set; } = 0;

        [SwaggerSchema(Title = "The number of pages", Nullable = true)]
        [Range(0, int.MaxValue)]
        public int? NumberOfPages { get; set; } = 0;

        [SwaggerSchema(Title = "The number of certified copies", Nullable = true)]
        [Range(0, int.MaxValue)]
        public int? NumberOfCertifiedCopies { get; set; } = 0;

        [SwaggerSchema(Title = "Is the request for a specified deadline", Nullable = true)]
        public bool? IsSpecified { get; set; } = false;

        [SwaggerSchema(Title = "Is the request free of charge", Nullable = true)]
        public bool? IsFoc { get; set; } = false;

        [SwaggerSchema(Title = "The number of changes", Nullable = true)]
        [Range(0, int.MaxValue)]
        public int? NumberOfChanges { get; set; } = 0;

        [SwaggerSchema(Title = "The number of patents", Nullable = true)]
        [Range(0, int.MaxValue)]
        public int? NumberOfPatents { get; set; } = 0;

        [SwaggerSchema(Title = "The number of trademarks", Nullable = true)]
        [Range(0, int.MaxValue)]
        public int? NumberOfTrademarks { get; set; } = 0;

        [SwaggerSchema(Title = "The number of designs", Nullable = true)]
        [Range(0, int.MaxValue)]
        public int? NumberOfDesigns { get; set; } = 0;

        [SwaggerSchema(Title = "The right id", Nullable = true)]
        [Range(0, int.MaxValue)]
        public int? RightId { get; set; } = null;

        [SwaggerSchema(Title = "The right type. Values: 'Patent', 'Trademark' or 'Design'", Nullable = true)]
        public string? RightType { get; set; } = null;

        [SwaggerSchema(Title = "The patent type. Values: 'GB' or 'EP'", Nullable = true)]
        public string? PatentType { get; set; } = null;

        [SwaggerSchema(Title = "The year count of the first chargable renewal (e.g. in year 5, year 6, year 7, etc)", Nullable = true)]
        [Range(0, int.MaxValue)]
        public int? FromRenewalYear { get; set; } = null;

        [SwaggerSchema(Title = "The year count of the current renewal (e.g. in year 5, year 6, year 7, etc)", Nullable = true)]
        [Range(0, int.MaxValue)]
        public int? RenewalYear { get; set; } = null;

        [SwaggerSchema(Title = "Is the renewal a late grant", Nullable = true)]
        public bool? IsLateGrant { get; set; } = null;

        [SwaggerSchema(Title = "Is the renewal a licence of right", Nullable = true)]
        public bool? IsLOR { get; set; } = null;

		[SwaggerSchema(Title = "Is the renewal a restoration", Nullable = true)]
		public bool IsRestoration { get; set; } = false;

		[SwaggerSchema(Title = "The date renewal is due", Format = "\"yyyy-MM-dd\"", Nullable = true)]
        public string? RenewalDueDate { get; set; }

        [SwaggerSchema(Title = "Is a declaration of priority that is late", Nullable = true)]
        public bool? IsLateDeclarationOfPriority { get; set; } = false;

        [SwaggerSchema(Title = "Is a translation that should be published", Nullable = true)]
        public bool? IsPublishTranslation { get; set; } = false;

        [SwaggerSchema(Title = "The number of years the right's protection should be extended by", Nullable = true)]
        public int SpcYear { get; set; } = 0;

        [SwaggerSchema(Title = "Is the request late?", Nullable = true)]
        public bool IsLate { get; set; } = false;

        [SwaggerSchema(Title = "Number of pages to be corrected?", Nullable = true)]
        public int? NumberOfPagesCorrection { get; set; } = 0;

        [SwaggerSchema(Title = "Number of claims to be corrected?", Nullable = true)]
        public int? NumberOfClaimsCorrection { get; set; } = 0;

		[SwaggerSchema(Title = "The date on which payment was made. Default = today(), Value copied from parent FeeCalculationRequest", Format = "\"yyyy-MM-dd\"", Nullable = true)]
		[JsonIgnore]
		public string PaidDate { get; set; } = DateTime.Now.ToString("yyyy-MM-dd");

        [SwaggerSchema(Title = "The date used to retrieve specific Fees", Format = "\"yyyy-MM-dd\"", Nullable = true)]
        public string? PaymentDate { get; set; }

        [SwaggerSchema(Title = "Indicates if the application fee has already been paid on the legacy system", Nullable = true)]
        public bool? IsApplicationFeePaid { get; set; } = false;
    }
}

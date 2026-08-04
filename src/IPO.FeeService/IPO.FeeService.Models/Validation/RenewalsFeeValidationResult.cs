namespace IPO.FeeService.Models.Validation
{
    public class RenewalsFeeValidationResult
    {
        public int Code { get; set; }
        public string? ErrorMessage { get; set; }

        public static RenewalsFeeValidationResult CreateSuccessValidationResult()
        {
            return new RenewalsFeeValidationResult()
            {
                Code = 200,
                ErrorMessage = string.Empty
            };
        }

        public static RenewalsFeeValidationResult CreateIsRequiredFieldValidationResult(string value)
        {
            return new RenewalsFeeValidationResult()
            {
                Code = 422,
                ErrorMessage = $"The request details could not be processed. The following mandatory field is missing: {value}"
            };
        }

		public static RenewalsFeeValidationResult CreateDateFormatValidationResult(string parameter, string value)
		{
			return new RenewalsFeeValidationResult()
			{
				Code = 422,
				ErrorMessage = $"The {parameter} value '{value}' could not be processed. A valid date must be provided and formatted as yyyy-MM-dd."
			};
		}
	}
}

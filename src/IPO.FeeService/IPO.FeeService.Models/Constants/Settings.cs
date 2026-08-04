namespace IPO.FeeService.Models.Constants
{
    public class Settings
    {
        public static readonly string[] SupportedChannels = { "00", "01", "02", "10" };

        public const string NoQuantityRuleValue = "No Rule Quantity = 1";

		public const string NoFeeRuleValue = "No Rule Effective Fee = 1";

		public static readonly string[] SupportedOperators = { "/", "*", "+", "-", "<=", "<", ">=", ">", "=" };
    }
}

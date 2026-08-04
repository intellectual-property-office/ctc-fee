using IPO.Common.Infrastructure;
using IPO.FeeService.Models.API;
using IPO.FeeService.Models.Constants;
using IPO.FeeService.Models.Data;
using IPO.FeeService.Services.Services;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace IPO.FeeService.Services.Calculations
{
    public abstract class CommonFeeCalculations
    {
        internal abstract void CalculateBreakDown(FeeCalculationRequest feeCalculationRequest, ServiceRequestType serviceRequest, string channel, DateTime paidDate, List<FeeItem> breakdown, bool isRenewals);

        internal abstract FeeItem CalculateLineItem(int lineNumber, Product product, Fee fee, TaxCode taxCode, int quantity, bool isRenewals, int productSequence);

        internal bool EvaluateFeeRule(FeeCalculationRequestDetails details, string rule)
        {
			if (string.Equals(rule, Settings.NoFeeRuleValue, StringComparison.Ordinal))
			{
				return true;
			}
			else
			{
				return ProcessComplexRule(details, rule) == 1;
			}
		}
        
        internal int CalculateQuantityForProduct(FeeCalculationRequestDetails details, QuantityRule quantityRule)
        {
            string ruleString = quantityRule?.Rule!;

            if (string.IsNullOrEmpty(ruleString))
            {
                var message = $"The quantity rule for this product is blank.";
                var error = Error.GetError<FeeManagementService>();
                error.Description = string.IsNullOrEmpty(error.Description) ? message : error.Description + $" {message}";
                throw new StatusCodeException(error, message, null!, StatusCodes.Status406NotAcceptable);
            }

            if (ruleString == Settings.NoQuantityRuleValue)
            {
                return 1;
            }
            else
			{
                return ProcessComplexRule(details, ruleString);
            }
        }

        internal int ProcessComplexRule(FeeCalculationRequestDetails details, string rule)
        {
			//Translate the rule
			if (rule.StartsWith("MAX")) //e.g. MAX(0,numberOfPages - 35)
			{
				return ApplyMaxRule(details, rule);
			}
			else if (rule.StartsWith("IF")) //e.g. IF(numberOfPatents >= 1, 1, 0)
			{
				return ApplyIfRule(details, rule);
			}
			else //e.g. "numberOfCertifiedCopies"
			{
				return (int)MapRuleToRequestProperty(details, rule);
			}
		}

        private object MapRuleToRequestProperty(FeeCalculationRequestDetails details, string rule)
        {
            try
            {
                string ruleInTitleCase = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(Regex.Replace(rule, "(?<=[a-z])([A-Z])", " $1", RegexOptions.Compiled)).Replace(" ", "") ?? string.Empty;

                var specialRuleResult = ApplySpecialRules(ruleInTitleCase);
                if (specialRuleResult != null) { return specialRuleResult; };

                var value = details.GetType().GetProperty(ruleInTitleCase)!.GetValue(details, null);

                //Special handling for bools
                if (value!.ToString()!.ToLower() == "true")
                {
                    return 1;
                }
                else if (value.ToString()!.ToLower() == "false")
                {
                    return 0;
                }

                return value;
            }
            catch (Exception)
            {
                var message = $"Unable to find match between rule and request details property: {rule}";
                var error = Error.GetError<FeeManagementService>();
                error.Description = string.IsNullOrEmpty(error.Description) ? message : error.Description + $" {message}";
                throw new StatusCodeException(error, message, null!, StatusCodes.Status406NotAcceptable);
            }
        }

        internal virtual object ApplySpecialRules(string ruleInTitleCase)
        {
            return null!;
        }

		private int ApplyMaxRule(FeeCalculationRequestDetails details, string rule)
		{
			return (int)ApplyMaxMinRule(details, rule, true);
		}

		private object ApplyMinRule(FeeCalculationRequestDetails details, string rule)
		{
			return ApplyMaxMinRule(details, rule, false);
		}

		private object ApplyMaxMinRule(FeeCalculationRequestDetails details, string rule, bool isMax)
		{
			// Separate the rule out into elements
			string argumentsOnly = rule.Replace(isMax ? "MAX(" : "MIN(", string.Empty).Replace(")", string.Empty).Replace(" ", string.Empty);

			var argumentsList = argumentsOnly.Split(',').ToList();
			if (argumentsList.Count != 2)
			{
				var message = $"Wrong number of arguments in quantity rule: {rule}.";
				var error = Error.GetError<FeeManagementService>();
				error.Description = string.IsNullOrEmpty(error.Description) ? message : error.Description + $" {message}";
				throw new StatusCodeException(error, message, null!, StatusCodes.Status406NotAcceptable);
			}

			// Obtain the argument values
			var firstArgumentValue = ProcessArgument(details, argumentsList[0]);
			var secondArgumentValue = ProcessArgument(details, argumentsList[1]);

			if (!DateTime.TryParseExact(firstArgumentValue.ToString(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date1))
			{
				// Perform the calculation for int
				int result = isMax ? Math.Max((int)firstArgumentValue, (int)secondArgumentValue) : Math.Min((int)firstArgumentValue, (int)secondArgumentValue);
				return result;
			}
			else
			{
				// Perform the calculation for dates
				DateTime.TryParseExact(secondArgumentValue.ToString(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date2);
				DateTime result = isMax ? (date1 > date2 ? date1 : date2) : (date1 < date2 ? date1 : date2);
				return result;
			}
		}

		private int ApplyIfRule(FeeCalculationRequestDetails details, string rule)
        {
            string withoutIf = rule.Replace("IF", string.Empty).Replace(" ", string.Empty); //remove IF
            string argumentsOnly = withoutIf.Substring(1, withoutIf.Length - 2); //remove enclosing brackets

            //Separate the rule out into elements
            var argumentsList = HandleNestedBrackets(argumentsOnly);
            if (argumentsList.Count != 3)
            {
                var message = $"Wrong number of arguments in quantity rule: {rule}.";
                var error = Error.GetError<FeeManagementService>();
                error.Description = string.IsNullOrEmpty(error.Description) ? message : error.Description + $" {message}";
                throw new StatusCodeException(error, message, null!, StatusCodes.Status406NotAcceptable);
            }

            //Obtain the argument values
            int condition = (int)ProcessArgument(details, argumentsList[0]);
            int firstValue, secondValue;
            if (argumentsList[1].StartsWith("MAX"))
            {
                firstValue = ApplyMaxRule(details, argumentsList[1]);
            }
            else
            {
                firstValue = (int)ProcessArgument(details, argumentsList[1]);
            }

            if (argumentsList[2].StartsWith("MAX"))
            {
                secondValue = ApplyMaxRule(details, argumentsList[2]);
            }
            else
            {
                secondValue = (int)ProcessArgument(details, argumentsList[2]);
            }

            //Perform the IF calculation
            int productQuantity = Convert.ToBoolean(condition) ? firstValue : secondValue;

            return productQuantity;
        }

        private object ProcessArgument(FeeCalculationRequestDetails details, string input)
        {
                if (int.TryParse(input, out int argumentValue)) //if int then return an int value
                {
                    return argumentValue;
                }
                else if (input.StartsWith("\"") && input.EndsWith("\"")) //if string then return a string value
                {
                    return input[1..^1];
                }

                // The argument must therefore contain a string which we need to handle
                string modifiedInput;
                if (input.Contains('('))
                {
                    string iteratedInput = input;
                    while (iteratedInput.Contains('('))
                    {
                        iteratedInput = HandleBrackets(details, iteratedInput);
                    }

                    modifiedInput = iteratedInput;
                    if (int.TryParse(modifiedInput, out int modifiedInputValue)) //if int then return an int value
                    {
                        return modifiedInputValue;
                    }
                }
                else
                {
                    modifiedInput = input;
                }

                foreach (string op in Settings.SupportedOperators)
                {
                    if (modifiedInput.Contains(op))
                    {
                        var argumentsList = modifiedInput.Split(op);
                        if (argumentsList.Length > 2)
                        {
                            var calculations = new List<int>();
                            foreach (string arg in argumentsList)
                            {
                                var value = ProcessArgument(details, arg);
                                calculations.Add(int.Parse(value.ToString()!));
                            }

                            int product = calculations.FirstOrDefault();
                            for (int i = 1; i < calculations.Count; i++)
                            {
                                int nextValue = calculations[i];
                                product = PerformOperation(product, nextValue, op);
                            }

                            return product;
                        }
                        else
                        {
                            var x = ProcessArgument(details, argumentsList[0]);
                            var y = ProcessArgument(details, argumentsList[1]);
                            return PerformOperation(x, y, op);
                        }
                    }
                }


                // If the argument doesn't contain an operator then just try to map the value to a property on request details
                return MapRuleToRequestProperty(details, modifiedInput);
            }

        private string HandleBrackets(FeeCalculationRequestDetails details, string input)
        {
            int startBracket = -1;
            int incompleteBracketPairs = 0;
            string output = input;
            for (int i = 0; i < input.Length; i++)
            {
                char c = input[i];
                if (c == '(')
                {
                    incompleteBracketPairs++;
                    if (startBracket == -1)
                    {
                        startBracket = i;
                    }
                }
                else if (c == ')')
                {
                    incompleteBracketPairs--;
                    if (incompleteBracketPairs == 0)
                    {
                        int startPosition = startBracket + 1;
                        int endPosition = i;
                        string bracketContents = input[startPosition..endPosition];

                        string newContents;
                        if (bracketContents.Contains('('))
						{
							if (bracketContents.StartsWith("MIN") && (bracketContents.Contains(">=") || bracketContents.Contains("<=")))
							{ //process the complex rule of MIN and GreaterThanOrEqualTo or LessThanOrEqualTo algebra logic i.e. MIN(paymentDate,renewalDueDate)>="06/04/2018"
								bool greaterThanOrEqual = bracketContents.Contains(">=");
								newContents = ProcessMinRuleWithDateComparison(details, bracketContents, greaterThanOrEqual);
							}
							//NOTE: this is working code if we ever need to add MIN or MAX nested within an IF rule.
							//else if (bracketContents.StartsWith("MAX") || bracketContents.StartsWith("MIN"))
							//{
							//	bool isMax = bracketContents.StartsWith("MAX");
							//	newContents = ApplyMaxMinRule(details, bracketContents, isMax).ToString();
							//}
							else
                            {
                                newContents = HandleBrackets(details, bracketContents).ToString();
                            }
                            output = output.Replace(bracketContents, newContents);
                        }
                        else
                        {
                            newContents = ProcessArgument(details, bracketContents).ToString()!;
                            output = output.Replace($"({bracketContents})", newContents);
                        }

                        startBracket = -1;
                    }
                }
            }
            return output; //This should see each bracket contents replaced by the evaluation of the expression within them
        }

        private static List<string> HandleNestedBrackets(string input)
        {
            var arguments = new List<string>();
            int start = 0, parentheses = 0;
            for (int i = 0; i < input.Length; i++)
            {
                if (input[i] == '(') parentheses++;
                else if (input[i] == ')') parentheses--;
                else if (input[i] == ',' && parentheses == 0)
                {
                    arguments.Add(input.Substring(start, i - start).Trim());
                    start = i + 1;
                }
            }
            arguments.Add(input.Substring(start).Trim()); // Add the last argument
            return arguments;
        }

        private static int PerformOperation(object x, object y, string op)
        {
            return op switch
            {
                "+" => (int)x + (int)y,
                "-" => (int)x - (int)y,
                "/" => (int)x / (int)y,
                "*" => (int)x * (int)y,
                "<=" => (int)x <= (int)y ? 1 : 0,
                ">=" => (int)x >= (int)y ? 1 : 0,
                "<" => (int)x < (int)y ? 1 : 0,
                ">" => (int)x > (int)y ? 1 : 0,
                "=" => x.ToString() == y.ToString() ? 1 : 0,
                _ => throw new StatusCodeException(Error.GetError<FeeManagementService>(), $"Unsupported operator '{op}'.", null!, StatusCodes.Status406NotAcceptable)
            };
        }

		private string ProcessMinRuleWithDateComparison(FeeCalculationRequestDetails details, string bracketContents, bool greaterThanOrEqual = true)
		{
			// Process the complex rule of MIN and algebra logic i.e. MIN(paidDate,renewalDueDate)>="06/04/2018"
			int index = bracketContents.IndexOf(')');
			string minContents = bracketContents.Substring(0, index + 1);
			DateTime date1 = (DateTime)ApplyMinRule(details, minContents);
			index = bracketContents.IndexOf('"');
			string date2string = bracketContents.Substring(index + 1, bracketContents.Length - index - 2);
			var date2 = DateTime.ParseExact(date2string, "dd/MM/yyyy", CultureInfo.InvariantCulture);
			int result = greaterThanOrEqual ? (date1 >= date2 ? 1 : 0) : (date1 <= date2 ? 1 : 0);
			return result.ToString();
		}
	}
}

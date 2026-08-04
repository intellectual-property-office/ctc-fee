using Swashbuckle.AspNetCore.Filters;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace IPO.FeeService.Models.API
{
    [ExcludeFromCodeCoverage]
    public class FeeInformationRequestExamples : IMultipleExamplesProvider<JsonDocument>
    {
        public IEnumerable<SwaggerExample<JsonDocument>> GetExamples()
        {
            yield return SwaggerExample.Create("ExampleRequest", ExampleRequest());
        } 
        private static JsonDocument ExampleRequest()
        {
            return JsonDocument.Parse(@"{
                          ""effectiveDate"": ""2026-01-01"",
                          ""productNumbers"": [ 1,2,3,197 ]
                          }");
        }
    }
}

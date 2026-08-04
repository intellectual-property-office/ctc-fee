using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPO.FeeService.BDDTests.Mocks
{
    public static class MockPayloads
    {
        public static readonly Dictionary<string, string> Payloads = new()
        {
            ["MalformedPayload"] = "{\r\n  \"\\eeffectiveDate\": \"2026-01-0\",\r\n  \"productNumbers[\r\n    1,\r\n    2,\r\n    3,\r\n    197,\r\n  ]\r\n}",

            ["ValidPayload"] = "{\r\n  \"effectiveDate\": \"2026-01-01\",\r\n  \"productNumbers\": [\r\n    1,\r\n    2,\r\n    3,\r\n    197\r\n  ]\r\n}",

            ["XmlPayload"] = "<FeeInformationRequest>\r\n  <EffectiveDate>2026-01-01</EffectiveDate>\r\n  <ProductNumbers>\r\n    <ProductNumber>1</ProductNumber>\r\n    <ProductNumber>2</ProductNumber>\r\n    <ProductNumber>3</ProductNumber>\r\n    <ProductNumber>197</ProductNumber>\r\n  </ProductNumbers>\r\n</FeeInformationRequest>\r\n",

            ["EmptyPayload"] = ""
        };
    }
}

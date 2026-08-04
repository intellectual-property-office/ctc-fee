using AwesomeAssertions;
using IPO.FeeService.BDDTests.Helpers;
using IPO.FeeService.BDDTests.Mocks;
using IPO.FeeService.Models.API;
using Microsoft.AspNetCore.TestHost;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Reqnroll;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace IPO.FeeService.BDDTests.StepDefinitions
{
    [Binding]
    public class FeeApiTestsStepDefinitions
    {
        private readonly ScenarioContext _scenarioContext;
        private readonly TestServer _server;
        private readonly HttpClient _client;

        public FeeApiTestsStepDefinitions(ScenarioContext scenarioContext)
        {
            _scenarioContext = scenarioContext;
            _server = TestStartup.GetTestServer();
            _client = _server.CreateClient();
        }

        [Given("I set the input media type to {string}")]
        public void GivenISetTheMediaTypeTo(string mediaType)
        {
            _scenarioContext["MediaType"] = mediaType;
        }

        [When(@"I call POST \/{string} with payload {string}")]
        public async Task WhenICallPOSTWithPayload(string route, string payloadName)
        {
            var payload = MockPayloads.Payloads[payloadName];
            var content = new StringContent(payload, Encoding.UTF8, (string)_scenarioContext["MediaType"]);
            var response = await _client.PostAsync($"/{route}", content);
            _scenarioContext["Response"] = response;
        }

            [Then("the response status should be {string}")]
        public void ThenTheResponseStatusShouldBe(string expectedStatus)
        {
            var response = (HttpResponseMessage)_scenarioContext["Response"];
            var actualStatus = response.StatusCode.ToString();

            actualStatus.Should().Be(expectedStatus);
        }

        [Then("for successful responses the output media type should be {string}")]
        public void ThenForSuccessfulResponsesTheMediaTypeShouldBe(string expectedMediaType)
        {
            var response = (HttpResponseMessage)_scenarioContext["Response"];
            var actualStatus = response.StatusCode.ToString();
            var successStatuses = new[] { "OK", "Created", "Accepted" };

            if (successStatuses.Contains(actualStatus))
            {
                var actualMediaType = response.Content.Headers.ContentType?.MediaType;
                actualMediaType.Should().Be(expectedMediaType);
            }
        }

        [Then("for successful responses, the response should contain header Content-Version with value matching the app version")]
        public void ThenForSuccessfulResponsesTheResponseShouldContainHeaderContent_VersionWithValueMatchingTheAppVersion()
        {
            var response = (HttpResponseMessage)_scenarioContext["Response"];
            var actualStatus = response.StatusCode.ToString();
            var successStatuses = new[] { "OK", "Created", "Accepted" };

            if (successStatuses.Contains(actualStatus))
            {
                response.Headers.TryGetValues("Content-Version", out var values);
                values.Should().NotBeNull();
                values.Should().Contain(TestStartup.Version!.FullVersion);
            }
        }

    }
}

Feature: CalculateFeeTests




Scenario Outline: Calculate Fee by Service Request Type
  Given I set the input media type to "<MediaType>"
  When I call POST /"calculate/<ServiceRequestType>?channel=<Channel>" with payload "<Payload>"
  Then the response status should be "<ExpectedStatus>"
  And for successful responses the output media type should be "application/json"
  And for successful responses, the response should contain header Content-Version with value matching the app version

Examples:
| ServiceRequestType | Channel | MediaType        | Payload             | ExpectedStatus       |
| Acceleration       | 00      | application/json | ValidPayload        | OK                   |
| Acceleration       | 00      | application/json | MalformedPayload    | UnprocessableEntity  |
| Acceleration       | 00      | application/json | EmptyPayload        | UnprocessableEntity  |
| Acceleration       | 00      | text/xml         | XmlPayload          | UnsupportedMediaType |
| Acceleration       |         | application/json | ValidPayload        | OK				   |
| InvalidType        | 00      | application/json | ValidPayload        | UnprocessableEntity  |


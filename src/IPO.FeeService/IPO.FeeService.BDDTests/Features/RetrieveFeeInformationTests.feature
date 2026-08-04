Feature: RetrieveFeeInformationTests




Scenario Outline: Retrieve Fee Information by Product Numbers
  Given I set the input media type to "<MediaType>"
  When I call POST /"byProductNumber<QueryParameters>" with payload "<Payload>"
  Then the response status should be "<ExpectedStatus>"
  And for successful responses the output media type should be "application/json"
  And for successful responses, the response should contain header Content-Version with value matching the app version

Examples:
| QueryParameters	| MediaType        | Payload             | ExpectedStatus       |
| ?channel=00		| application/json | ValidPayload        | OK                   |
| ?channel=00		| application/json | MalformedPayload    | UnprocessableEntity  |
| ?channel=00		| application/json | EmptyPayload        | UnprocessableEntity  |
| ?channel=00		| text/xml         | XmlPayload          | UnsupportedMediaType |
|					| application/json | ValidPayload        | OK					|
| ?channel=01		| application/json | ValidPayload        | OK					|


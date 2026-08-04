# Fee Microservice

# About
The Fee Microservice performs the calculation of fee costs for different types of service requests provided by the IPO.

# Installation guide
### System Requirements
- IDE capable of running .NET 10 or above i.e. Visual Studio

### Installation instructions
1. Clone the repository to your local machine.

2. Open the 'IPO.FeeService.sln' solution file in Visual Studio.

3. Ensure that the settings file called 'appsettings.json' in the IPO.FeeService.API project matches the contents of the below Configuration file.

4. Build the solution.

5. Set the Web API (IPO.FeeService.API) project as the Startup project in Visual Studio and run in debug configuration.

6. A command window will launch, in which you will see the Console output.

7. The swagger page will launch in your default browser ready to test the endpoints.

## Configuration files
IPO.FeeService.API
```JSON
{
  "IpoLogLevel": "Error",
  "AllowedHosts": "*",
  "FeeDbConnection": ""
}
```


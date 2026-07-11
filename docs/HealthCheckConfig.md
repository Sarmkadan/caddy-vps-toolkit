# HealthCheckConfig
The `HealthCheckConfig` type is designed to hold configuration settings for a health check, which is a mechanism used to monitor the status of a service or endpoint. It provides a set of properties that define how the health check should be performed, including the type of check, the endpoint to check, and the criteria for determining whether the check is successful.

## API
### Properties
* `Id`: A unique identifier for the health check configuration.
* `ServiceId`: The identifier of the service being monitored.
* `Type`: The type of health check to perform, represented by the `HealthCheckType` enum.
* `Endpoint`: The URL or endpoint to check for health.
* `IntervalSeconds`: The interval, in seconds, between health checks.
* `TimeoutSeconds`: The maximum time, in seconds, to wait for a response before considering the check failed.
* `UnhealthyThreshold`: The number of consecutive failed checks required to consider the service unhealthy.
* `HealthyThreshold`: The number of consecutive successful checks required to consider the service healthy.
* `Enabled`: A boolean indicating whether the health check is currently enabled.
* `HttpMethod`: The HTTP method to use when performing the health check (e.g., GET, POST).
* `ExpectedHttpStatus`: The expected HTTP status code for a successful health check.
* `ExpectedResponse`: The expected response content for a successful health check.
* `CreatedAt` and `UpdatedAt`: Timestamps indicating when the health check configuration was created and last updated, respectively.

### Methods
* `Validate()`: Validates the health check configuration, throwing an exception if any settings are invalid.
* `GetHealthCheckUrl()`: Returns the URL that should be used to perform the health check, based on the `Endpoint` and other configuration settings.

## Usage
The following examples demonstrate how to use the `HealthCheckConfig` type:
```csharp
// Example 1: Creating a basic health check configuration
var config = new HealthCheckConfig
{
    Id = "example-check",
    ServiceId = "example-service",
    Type = HealthCheckType.Http,
    Endpoint = "https://example.com/health",
    IntervalSeconds = 30,
    TimeoutSeconds = 5,
    UnhealthyThreshold = 3,
    HealthyThreshold = 2,
    Enabled = true,
    HttpMethod = "GET",
    ExpectedHttpStatus = 200
};

// Example 2: Validating and using a health check configuration
var config2 = new HealthCheckConfig
{
    Id = "example-check-2",
    ServiceId = "example-service-2",
    Type = HealthCheckType.Http,
    Endpoint = "https://example2.com/health",
    IntervalSeconds = 60,
    TimeoutSeconds = 10,
    UnhealthyThreshold = 5,
    HealthyThreshold = 3,
    Enabled = true,
    HttpMethod = "GET",
    ExpectedHttpStatus = 200
};

config2.Validate();
var healthCheckUrl = config2.GetHealthCheckUrl();
Console.WriteLine($"Health check URL: {healthCheckUrl}");
```

## Notes
When using the `HealthCheckConfig` type, consider the following:
* The `Validate` method should be called before using a health check configuration to ensure that all settings are valid.
* The `GetHealthCheckUrl` method returns a URL based on the `Endpoint` and other configuration settings; this URL should be used when performing the health check.
* The `HealthCheckConfig` type is not thread-safe; if multiple threads need to access or modify a health check configuration, appropriate synchronization mechanisms should be used.
* The `ExpectedResponse` property can be used to specify a expected response content for a successful health check, but it is not used by default; to use this feature, additional logic must be implemented to compare the actual response content with the expected content.

# IServiceRepository

`IServiceRepository` is a read-only interface that provides aggregated statistics and metrics about service checks performed by the Caddy VPS Toolkit. It exposes key performance indicators such as success rates, response times, and check counts to enable monitoring and alerting on service health.

## API

### `public int TotalChecks`
Returns the total number of service checks performed.
- **Returns**: The total count of checks, always non-negative.
- **Throws**: Never throws exceptions.

### `public int SuccessfulChecks`
Returns the number of service checks that completed successfully.
- **Returns**: The count of successful checks, always non-negative and less than or equal to `TotalChecks`.
- **Throws**: Never throws exceptions.

### `public int FailedChecks`
Returns the number of service checks that failed.
- **Returns**: The count of failed checks, always non-negative and equal to `TotalChecks - SuccessfulChecks`.
- **Throws**: Never throws exceptions.

### `public double SuccessRate`
Returns the success rate of service checks as a value between 0.0 and 1.0.
- **Returns**: The success rate, calculated as `SuccessfulChecks / TotalChecks`.
- **Throws**: Never throws exceptions. If `TotalChecks` is zero, returns 0.0.

### `public int AverageResponseTimeMs`
Returns the average response time of all service checks in milliseconds.
- **Returns**: The average response time, rounded to the nearest millisecond. If no checks have been performed, returns 0.
- **Throws**: Never throws exceptions.

### `public int MaxResponseTimeMs`
Returns the maximum response time observed among all service checks in milliseconds.
- **Returns**: The highest response time recorded. If no checks have been performed, returns 0.
- **Throws**: Never throws exceptions.

### `public int MinResponseTimeMs`
Returns the minimum response time observed among all service checks in milliseconds.
- **Returns**: The lowest response time recorded. If no checks have been performed, returns 0.
- **Throws**: Never throws exceptions.

## Usage

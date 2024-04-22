# ISslCertificateMonitoringService

Provides asynchronous monitoring and renewal alerting for SSL certificates across configured services. This interface enables periodic checks of certificate validity, expiration warnings, and automated renewal notifications to maintain secure HTTPS endpoints.

## API

### `CheckCertificateAsync`

Checks the validity and expiration status of a single SSL certificate.

- **Parameters**
  - None
- **Return value**
  - `Task<Result<SslCertificateCheckResult>>`: A task that resolves to a `Result` containing either a `SslCertificateCheckResult` with the certificate's status or an error indicating the failure reason.
- **Exceptions**
  - Throws `InvalidOperationException` if the service is not properly configured for the target endpoint.
  - Throws `HttpRequestException` if the remote endpoint cannot be reached or returns an invalid response.

---

### `CheckAllServicesAsync`

Checks the SSL certificates for all configured services in the monitoring scope.

- **Parameters**
  - None
- **Return value**
  - `Task<IReadOnlyList<SslCertificateCheckResult>>`: A task that resolves to a read-only list of `SslCertificateCheckResult` objects, one per monitored service. Each result includes validity and expiration details.
- **Exceptions**
  - Throws `InvalidOperationException` if no services are configured or the monitoring service is in an invalid state.
  - Throws `AggregateException` containing one or more inner exceptions if any individual service check fails.

---
### `SendRenewalAlertsAsync`

Sends notifications (e.g., email, webhook, or log) for certificates that require renewal based on configured thresholds.

- **Parameters**
  - None
- **Return value**
  - `Task`: A task that completes when all alerts have been dispatched. Does not throw on individual failures; logs errors internally.
- **Exceptions**
  - Throws `InvalidOperationException` if no alerting channels are configured or the service lacks permissions to send notifications.

## Usage

### Example 1: Periodic certificate check in a background service

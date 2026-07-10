# SslCertStatusCheckerTests

Unit tests for the `SslCertStatusChecker` class, verifying SSL certificate validation logic, status reporting, and edge-case handling. The tests cover certificate expiration checks, domain validation, and result modeling for service monitoring scenarios.

## API

### `SslCertStatusCheckerTests()`

Constructor for the test class. Initializes test fixtures and mock services required for SSL certificate validation scenarios.

### `async Task CheckAllServicesAsync_SkipsLocalHostBindings()`

Verifies that the `SslCertStatusChecker` correctly bypasses localhost bindings when checking service certificates. Ensures no validation attempts are made against `localhost`, `127.0.0.1`, or `::1` addresses.

### `async Task CheckCertificateAsync_WithEmptyDomain_ReturnsFailure()`

Tests that attempting to validate an SSL certificate with an empty domain string results in a failed check. Confirms proper error handling and status reporting when invalid input is provided.

### `void SslCertificateCheckResult_CreateValid_SetsCorrectStatus()`

Ensures that creating a `SslCertificateCheckResult` for a valid certificate sets the `Status` property to `Ok`. Validates correct state initialization for successful certificate checks.

### `void SslCertificateCheckResult_CreateExpired_SetsCorrectStatus()`

Confirms that creating a `SslCertificateCheckResult` for an expired certificate sets the `Status` property to `Expired`. Validates correct state initialization for expired certificate scenarios.

### `void SslCertificateCheckResult_CreateExpiringSoon_Critical_SetsCorrectStatus()`

Ensures that creating a `SslCertificateCheckResult` for a certificate expiring soon (with critical threshold) sets the `Status` property to `Critical`. Validates correct state initialization for near-expiry certificate checks.

### `void SslCertificateCheckResult_CreateError_SetsErrorStatus()`

Confirms that creating a `SslCertificateCheckResult` with an error condition sets the `Status` property to `Error`. Validates correct state initialization for failed certificate checks.

### `void SslCertificateInfo_DaysUntilExpiry_ReturnsZeroForExpiredCert()`

Ensures that the `DaysUntilExpiry` property of `SslCertificateInfo` returns `0` when the certificate has already expired. Validates correct calculation of remaining validity period.

## Usage

# CaddyConfigPipelineIntegrationTests

Integration tests for the Caddy configuration pipeline, verifying end-to-end behavior of route generation, JSON serialization, and Caddyfile validation. These tests exercise the complete pipeline from service definitions to a valid Caddyfile, ensuring correct handling of active/inactive routes, headers, rate limits, authentication, TLS providers, and path transformations.

## API

### `CaddyConfigPipelineIntegrationTests`
Test class containing integration tests for the Caddy configuration pipeline.

### `async Task GenerateCaddyfileAsync_WithMultipleActiveRoutes_IncludesAllRoutes()`
Verifies that when multiple routes are active, the generated Caddyfile includes all route blocks. No parameters. Returns `Task`. Throws if route processing fails or if any route is unexpectedly excluded.

### `async Task GenerateCaddyfileAsync_InactiveRoutesAreExcluded()`
Ensures that routes marked as inactive are omitted from the generated Caddyfile. No parameters. Returns `Task`. Throws if inactive routes appear in output or if active routes are missing.

### `async Task GenerateCaddyfileAsync_WithNullRoutesList_ReturnsFallbackComment()`
Validates that passing `null` as the routes list results in a fallback comment in the Caddyfile instead of an exception. No parameters. Returns `Task`. Throws only if the fallback mechanism itself fails.

### `async Task GenerateCaddyfileAsync_WithEmptyRoutesList_ReturnsFallbackComment()`
Ensures that an empty routes list produces a fallback comment in the Caddyfile. No parameters. Returns `Task`. Throws only if the fallback mechanism fails.

### `async Task GenerateCaddyfileAsync_GlobalsArePresentInOutput()`
Checks that global Caddy configuration (e.g., logging, admin settings) is included in the generated Caddyfile. No parameters. Returns `Task`. Throws if global settings are missing or malformed.

### `void GenerateRouteBlock_RootPath_GeneratesSimpleReverseProxy()`
Confirms that a route with a root path (`/`) generates a minimal reverse proxy block without a named matcher. No parameters. Returns `void`. Throws if the generated block does not match expected syntax.

### `void GenerateRouteBlock_NonRootPath_GeneratesNamedMatcher()`
Ensures that a route with a non-root path generates a named matcher in the Caddyfile. No parameters. Returns `void`. Throws if the matcher is missing or incorrectly named.

### `void GenerateRouteBlock_NonRootPath_HyphensReplacedInMatcherName()`
Validates that hyphens in non-root paths are replaced with underscores in the matcher name. No parameters. Returns `void`. Throws if hyphens remain in the matcher name.

### `void GenerateRouteBlock_WithCustomHeaders_EmitsHeaderDirectives()`
Checks that custom headers defined in a route are emitted as `header` directives in the Caddyfile. No parameters. Returns `void`. Throws if headers are missing or malformed.

### `void GenerateRouteBlock_WithRateLimitRule_EmitsRateLimitDirective()`
Ensures that a rate limit rule in a route results in a `rate_limit` directive in the Caddyfile. No parameters. Returns `void`. Throws if the directive is missing or incorrect.

### `void GenerateRouteBlock_WithBasicAuth_EmitsBasicauthBlock()`
Verifies that a route with basic authentication emits a `basicauth` block in the Caddyfile. No parameters. Returns `void`. Throws if the block is missing or malformed.

### `void GenerateRouteBlock_WithTlsDnsProvider_EmitsTlsDnsBlock()`
Confirms that a route with a TLS DNS provider emits the corresponding `tls` block with the DNS provider configuration. No parameters. Returns `void`. Throws if the block is missing or incorrect.

### `void GenerateRouteBlock_WithStripPath_EmitsStripPrefixDirective()`
Ensures that a route with `strip_path` set emits a `strip_prefix` directive in the Caddyfile. No parameters. Returns `void`. Throws if the directive is missing or incorrect.

### `async Task ValidateCaddyfileAsync_WellFormedContent_ReturnsTrue()`
Tests that a well-formed Caddyfile passes validation without throwing. No parameters. Returns `Task<bool>`. Throws `CaddyOperationException` if the file is malformed.

### `async Task ValidateCaddyfileAsync_UnmatchedOpenBrace_ThrowsCaddyOperationException()`
Validates that a Caddyfile with an unmatched open brace throws `CaddyOperationException`. No parameters. Returns `Task`. Throws `CaddyOperationException` with details of the syntax error.

### `async Task ValidateCaddyfileAsync_UnmatchedClosingBrace_ThrowsCaddyOperationException()`
Ensures that a Caddyfile with an unmatched closing brace throws `CaddyOperationException`. No parameters. Returns `Task`. Throws `CaddyOperationException` with details of the syntax error.

### `void GenerateCaddyJsonAsync_WithActiveRoutes_ProducesValidJsonStructure()`
Checks that the generated JSON configuration for active routes is valid and contains expected fields. No parameters. Returns `void`. Throws if JSON is invalid or missing required fields.

### `void GenerateRouteForService_PopulatesAllExpectedFields()`
Verifies that all expected fields in a route derived from a service definition are populated correctly. No parameters. Returns `void`. Throws if any field is missing or incorrect.

### `async Task FullPipeline_ServiceToValidCaddyfile_ProducesBalancedBraces()`
Tests the full pipeline from service definitions to Caddyfile, ensuring the output has balanced braces and is syntactically valid. No parameters. Returns `Task`. Throws if braces are unbalanced or if any intermediate step fails.

## Usage

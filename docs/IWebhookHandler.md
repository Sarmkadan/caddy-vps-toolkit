# IWebhookHandler

The `IWebhookHandler` interface defines the contract for registering, unregistering, and triggering webhook events in the `caddy-vps-toolkit` project. It provides methods to manage webhook registrations, trigger events asynchronously, and retrieve metadata about triggered events.

## API

### `WebhookHandler`

The `WebhookHandler` type is a delegate representing the method signature for handling webhook events. It is defined as:

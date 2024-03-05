# EventBusTests

Unit tests for the `EventBus` class, verifying subscription, unsubscription, and event publishing behavior under various scenarios including null checks, concurrency, and type safety.

## API

### `PublishAsync_WithSubscriber_InvokesHandler`
Ensures that when an event is published, any subscribed handler is invoked exactly once. No parameters. No return value. Does not throw.

### `PublishAsync_NoSubscribers_DoesNotThrow`
Verifies that publishing an event with no subscribers completes successfully without raising exceptions. No parameters. No return value. Does not throw.

### `PublishAsync_NullEvent_ThrowsArgumentNullException`
Confirms that passing a null event to `PublishAsync` results in an `ArgumentNullException`. No parameters. No return value. Throws `ArgumentNullException` when the event argument is null.

### `PublishAsync_MultipleSubscribers_AllHandlersInvoked`
Validates that when multiple handlers are subscribed to the same event type, all are invoked during a single publish. No parameters. No return value. Does not throw.

### `Unsubscribe_RemovesHandler_NotInvokedOnNextPublish`
Checks that after unsubscribing a handler, it is no longer invoked on subsequent publishes. No parameters. No return value. Does not throw.

### `Unsubscribe_NullHandler_DoesNotThrow`
Ensures that attempting to unsubscribe a null handler does not throw an exception. No parameters. No return value. Does not throw.

### `Subscribe_NullHandler_ThrowsArgumentNullException`
Confirms that subscribing a null handler results in an `ArgumentNullException`. No parameters. No return value. Throws `ArgumentNullException` when the handler argument is null.

### `GetSubscriberCount_NoSubscribers_ReturnsZero`
Verifies that the subscriber count is zero when no handlers are subscribed. No parameters. Returns `0`.

### `GetSubscriberCount_AfterSubscription_ReturnsCorrectCount`
Ensures that after a handler is subscribed, `GetSubscriberCount` reflects the correct count. No parameters. Returns the current number of subscribed handlers.

### `GetSubscriberCount_AfterUnsubscribe_DecreasesCount`
Checks that unsubscribing a handler reduces the count returned by `GetSubscriberCount`. No parameters. Returns the updated subscriber count.

### `PublishAsync_DifferentEventTypes_OnlyCorrectHandlerInvoked`
Validates that when publishing an event, only handlers subscribed to that specific event type are invoked. No parameters. No return value. Does not throw.

### `PublishAsync_ConcurrentPublishes_AllHandlersInvoked`
Ensures that concurrent calls to `PublishAsync` result in all subscribed handlers being invoked for their respective events. No parameters. No return value. Does not throw.

## Usage

# ServiceCreatedEventHandlerValidation

`ServiceCreatedEventHandlerValidation` is a static class of extension methods that check a `ServiceCreatedEventHandler` and return a list of problems. It offers three entry points: `Validate` returns every problem found, `IsValid` returns a simple boolean, and `EnsureValid` throws an `ArgumentException` that lists all problems. All three are defined in the `CaddyVpsToolkit.Events` namespace, so the `using CaddyVpsToolkit.Events;` directive is enough to call them.

The checks do not inspect the handler's state. They use reflection to confirm that the private fields `_logger` and `_webhookHandler` exist on the `ServiceCreatedEventHandler` type. A handler is therefore reported as valid whenever the type still has those fields, no matter what values the fields hold. The validation guards against the class being refactored in a way that breaks code relying on those fields.

## API

### `Validate(this ServiceCreatedEventHandler value)`
Validates a `ServiceCreatedEventHandler` and collects every problem found. Validation does not stop at the first failure.

The following checks run, in this order:

| Check | Rule | Message |
|-------|------|---------|
| Logger field | The type must declare a private instance field named `_logger`. | `Logger field '_logger' not found in ServiceCreatedEventHandler.` |
| Webhook handler field | The type must declare a private instance field named `_webhookHandler`. | `Webhook handler field '_webhookHandler' not found in ServiceCreatedEventHandler.` |

**Parameters:**
- `value` (`ServiceCreatedEventHandler`) – The handler to validate. Cannot be `null`.

**Returns:**
`IReadOnlyList<string>` – A read-only list of validation messages. The list is empty if the handler is valid.

**Throws:**
`ArgumentNullException` – If `value` is `null`. The parameter name is `value`.

### `IsValid(this ServiceCreatedEventHandler value)`
Returns whether the handler passes every check in `Validate`. It is equivalent to `value.Validate().Count == 0`.

**Parameters:**
- `value` (`ServiceCreatedEventHandler`) – The handler to check. Cannot be `null`.

**Returns:**
`bool` – `true` if the handler has no validation problems; otherwise, `false`.

**Throws:**
`ArgumentNullException` – If `value` is `null`. The parameter name is `value`.

### `EnsureValid(this ServiceCreatedEventHandler value)`
Validates the handler and throws if any problem is found. Use it at startup or in tests, where a broken handler should stop the process rather than be handled at runtime.

**Parameters:**
- `value` (`ServiceCreatedEventHandler`) – The handler to validate. Cannot be `null`.

**Returns:**
`void`

**Throws:**
`ArgumentNullException` – If `value` is `null`. The parameter name is `value`.

`ArgumentException` – If the handler is invalid. The exception message has the form `ServiceCreatedEventHandler is not valid. Validation errors: <problem 1> <problem 2>`, listing every problem from `Validate` in order, separated by single spaces. The parameter name is not set.

## Usage

### Example 1: Collecting all problems

```csharp
using System;
using System.Collections.Generic;
using CaddyVpsToolkit.Events;

var handler = new ServiceCreatedEventHandler(logger, webhookHandler);

IReadOnlyList<string> problems = handler.Validate();

foreach (var problem in problems)
{
    Console.WriteLine(problem);
}
// Prints nothing for a handler built by the constructor, because both fields exist.
```

`logger` and `webhookHandler` are existing `ILogger` (`CaddyVpsToolkit.Middleware`) and `IWebhookHandler` (`CaddyVpsToolkit.Integration`) instances. The constructor rejects `null` for either one, so a handler created this way always has both fields set.

### Example 2: Checking validity with a boolean

```csharp
using System;
using CaddyVpsToolkit.Events;

if (!handler.IsValid())
{
    Console.Error.WriteLine("ServiceCreatedEventHandler failed validation; skipping registration.");
    return;
}

// Safe to register the handler with the event bus.
```

### Example 3: Rejecting an invalid handler with an exception

```csharp
using System;
using CaddyVpsToolkit.Events;

try
{
    handler.EnsureValid();
}
catch (ArgumentException ex)
{
    Console.Error.WriteLine(ex.Message);
}
```

`ArgumentNullException` derives from `ArgumentException`, so this `catch` also handles a `null` handler. Catch `ArgumentNullException` first if the two cases need different handling.

### Example 4: Calling the static methods directly

The extension methods are also static methods on `ServiceCreatedEventHandlerValidation`, so they can be called without the `this` syntax:

```csharp
using CaddyVpsToolkit.Events;

bool valid = ServiceCreatedEventHandlerValidation.IsValid(handler);
```

### Example 5: Validating at startup

```csharp
using CaddyVpsToolkit.Events;

// Fails fast if a refactoring removed or renamed _logger or _webhookHandler.
ServiceCreatedEventHandler handler = new(logger, webhookHandler);
handler.EnsureValid();
```

## Notes

- The checks run on every call, but the outcome depends only on the type's fields, not on the instance. Two handlers with different loggers return the same result.
- `Validate` returns a new list on every call. The returned list is a read-only view over that list, so callers cannot change the result of a later call.
- Both field lookups use `BindingFlags.NonPublic | BindingFlags.Instance`. A public field or property with the same name does not satisfy the check.
- A missing field is reported as a validation message, not as an exception. `Validate` never throws for a non-null handler.
- The class holds no mutable state. Its static fields are read-only and set once when the type is first used, so concurrent calls are safe.
- The field lookups run once, when the static class is first initialized. Each `Validate` call only checks whether the cached `FieldInfo` values are `null`, so repeated calls do not repeat the reflection lookup.
- `IsValid` and `EnsureValid` both call `Validate`, so they always report the same result as `Validate`.

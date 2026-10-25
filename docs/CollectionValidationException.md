# CollectionValidationException

`Core/CollectionValidationException.cs` defines `CollectionValidationException<T>`, an exception for batch operations where some items in a collection fail. It lives in the `CaddyVpsToolkit.Core` namespace and derives from [CaddyVpsException](CaddyVpsException.md). It is `sealed` and generic over the item type `T`.

Unlike the single-cause types in [CoreExceptions](CoreExceptions.md), this exception carries a list of every failed item, with its error and optional index. The caller gets the full picture of a partial failure instead of only the first error. Its fixed error code is `COLLECTION_VALIDATION_ERROR`.

> **Status:** At the time of writing, no code in the repository constructs this exception. Besides its own definition, the only reference is the `<exception cref>` tags on `ProcessAsync` in `Processing/BatchProcessor.cs`. `BatchProcessor` does not throw it. The examples below show how to use it. A batch processor that wants to report partial failures this way would have to throw it explicitly.

## API

### `public sealed class CollectionValidationException<T> : CaddyVpsException`
Thrown when one or more items in a collection fail validation or processing.

**Type parameter:**
- `T`: The type of the items in the collection. It is used as the `Item` element of each failed-item tuple.

---

### `public IReadOnlyList<(T Item, Exception Error, int? Index)> FailedItems { get; }`
The failed items, in the order they were supplied.

- `Item`: the item that failed.
- `Error`: the exception that describes the failure.
- `Index`: the position of the item in the source collection, or `null` when the position is not known.

It is set from the `failedItems` constructor argument. It is never `null`.

---

### `public int TotalItems { get; }`
The total number of items that were processed or attempted. It is set from the `totalItems` constructor argument.

---

### `public int SuccessCount { get; }`
The number of items that were processed successfully. It is set from the `successCount` constructor argument. The constructor does not check it against `TotalItems` or `FailureCount`, so the caller is responsible for keeping the three numbers consistent.

---

### `public int FailureCount { get; }`
The number of failed items. It is computed as `FailedItems.Count`, not taken from a constructor argument.

---

### `public bool AllSucceeded { get; }`
`true` when `FailureCount` is `0`. It is computed on each read.

---

### `public CollectionValidationException(string message, IReadOnlyList<(T Item, Exception Error, int? Index)> failedItems, int totalItems, int successCount)`
Creates an exception for a batch that partly failed.

**Parameters:**
- `message` (`string`): The error message.
- `failedItems` (`IReadOnlyList<(T Item, Exception Error, int? Index)>`): The failed items with their errors and indices. Pass an empty list if nothing failed.
- `totalItems` (`int`): The number of items that were processed.
- `successCount` (`int`): The number of items that succeeded.

**Result:**
- `Message`: the `message` value as passed
- `ErrorCode`: `COLLECTION_VALIDATION_ERROR`
- `Details`: an anonymous object with two members, `FailedCount` (the count of `failedItems`) and `TotalItems` (the value of `totalItems`)
- `FailedItems`, `TotalItems`, `SuccessCount`: the values as passed

See [Notes](#notes) for what happens when `failedItems` is `null`.

---

### `public override string ToString()`
Returns the base `CaddyVpsException` text followed by one line per failed item.

The base text has this form:

`CaddyVpsException { ErrorCode = COLLECTION_VALIDATION_ERROR, Details = { FailedCount = 2, TotalItems = 5 } }`

When `FailedItems` is not empty, the method appends a header and one line per failure:

```
Failed items (2/5):
- Item: <item>, Error: <error message>
- Item: <item>, Error: <error message> at index [3]
```

The `at index [n]` suffix appears only when `Index` has a value. The message itself is not included in the output, as in the base type.

---

## Usage

### Example 1: Throwing after a batch with failures
A batch processor that keeps going after an error can collect the failures and throw once at the end:

```csharp
using System;
using System.Collections.Generic;
using CaddyVpsToolkit.Core;

var hosts = new[] { "blog.example.com", "shop.example.com", "api.example.com" };
var failures = new List<(string Item, Exception Error, int? Index)>();
var successCount = 0;

for (var i = 0; i < hosts.Length; i++)
{
    try
    {
        ProvisionCertificate(hosts[i]);
        successCount++;
    }
    catch (Exception ex)
    {
        failures.Add((hosts[i], ex, i));
    }
}

if (failures.Count > 0)
{
    throw new CollectionValidationException<string>(
        "Certificate provisioning failed for some hosts",
        failures,
        totalItems: hosts.Length,
        successCount: successCount);
}
```

If `shop.example.com` is the only host that fails, the thrown exception has these values:
- `FailureCount`: `1`
- `SuccessCount`: `2`
- `AllSucceeded`: `false`
- `Details`: `{ FailedCount = 1, TotalItems = 3 }`

### Example 2: Reading the failed items
Callers can use the failed-item tuples to report each problem and to retry only the failed items:

```csharp
using System;
using System.Linq;
using CaddyVpsToolkit.Core;
using CaddyVpsToolkit.Domain.Models;

try
{
    RunBatch(services);
}
catch (CollectionValidationException<ManagedService> ex)
{
    Console.WriteLine($"{ex.SuccessCount} of {ex.TotalItems} succeeded, {ex.FailureCount} failed.");

    foreach (var (service, error, index) in ex.FailedItems)
    {
        var position = index.HasValue ? $" (position {index.Value})" : string.Empty;
        Console.WriteLine($"- {service.Id}{position}: {error.Message}");
    }

    // Retry only the services that failed
    var retry = ex.FailedItems.Select(f => f.Item).ToList();
    RunBatch(retry);
}
```

### Example 3: Branching on the error code
The error code is the same for every `T`, so a handler for the base type can match it without knowing the item type:

```csharp
using CaddyVpsToolkit.Core;

try
{
    RunBatch(services);
}
catch (CaddyVpsException ex) when (ex.ErrorCode == "COLLECTION_VALIDATION_ERROR")
{
    // Partial failure: log the details, keep the service running
    logger.LogWarning("{Message}", ex.ToString());
}
```

---

## Notes

- **Null `failedItems`:** The constructor's `?? throw new ArgumentNullException(...)` check is never reached for `null`. The base-constructor call reads `failedItems.Count` to build `Details` before the constructor body runs. A `null` argument therefore throws `NullReferenceException`, not `ArgumentNullException`. Always pass a non-null list.
- **Generic type and the base handler:** Because `CollectionValidationException<T>` is generic, `catch (CollectionValidationException<T>)` matches only the exact item type. A handler for the base type, `CaddyVpsException`, matches every `T`. Use the error code, as in Example 3, when the item type does not matter.
- **Counts are not cross-checked:** `TotalItems`, `SuccessCount`, and `FailureCount` are not validated against one another. `Details.TotalItems` is the same value as `TotalItems`, but `Details.FailedCount` comes from the list, so the two may disagree if the caller passes inconsistent numbers.
- **Inner exception:** The constructor does not take an inner exception. Each failure's own exception is stored in the `Error` element of its tuple.

# CollectionExtensionsTests

`CollectionExtensionsTests` is the unit test suite for the `CollectionExtensions` utility class in the `caddy-vps-toolkit` project. It validates the correctness, edge-case handling, and exception-throwing behavior of extension methods designed to operate on `IList<T>` and `IEnumerable<T>` collections. The tests cover safe indexed access, null/empty checks, batching, partitioning, conditional removal, and conditional bulk insertion.

## API

The test class exposes the following public test methods. Each method name describes the scenario under test and the expected outcome.

### SafeGet_ValidIndex_ReturnsElement
**Purpose:** Verifies that `SafeGet` returns the element at the specified index when the index is within the valid range of the list.
**Parameters:** None (parameterless test method).
**Returns:** `void` (asserts pass/fail internally).
**Throws:** Does not throw; the method under test must not throw.

### SafeGet_IndexOutOfRange_ReturnsDefault
**Purpose:** Verifies that `SafeGet` returns the provided default value when the index is greater than or equal to the list’s count.
**Parameters:** None.
**Returns:** `void`.
**Throws:** Does not throw; the method under test must not throw.

### SafeGet_NegativeIndex_ReturnsDefault
**Purpose:** Verifies that `SafeGet` returns the provided default value when a negative index is supplied.
**Parameters:** None.
**Returns:** `void`.
**Throws:** Does not throw; the method under test must not throw.

### SafeGet_NullList_ReturnsProvidedDefault
**Purpose:** Verifies that `SafeGet` returns the explicitly provided default value when the source list itself is `null`, without attempting to access it.
**Parameters:** None.
**Returns:** `void`.
**Throws:** Does not throw; the method under test must not throw a `NullReferenceException`.

### IsNullOrEmpty_NullCollection_ReturnsTrue
**Purpose:** Confirms that `IsNullOrEmpty` returns `true` when the input collection is `null`.
**Parameters:** None.
**Returns:** `void`.
**Throws:** Does not throw.

### IsNullOrEmpty_EmptyCollection_ReturnsTrue
**Purpose:** Confirms that `IsNullOrEmpty` returns `true` when the input collection is non-null but contains zero elements.
**Parameters:** None.
**Returns:** `void`.
**Throws:** Does not throw.

### IsNullOrEmpty_NonEmptyCollection_ReturnsFalse
**Purpose:** Confirms that `IsNullOrEmpty` returns `false` when the input collection contains at least one element.
**Parameters:** None.
**Returns:** `void`.
**Throws:** Does not throw.

### Batch_EvenDivision_ProducesCorrectBatches
**Purpose:** Verifies that `Batch` splits a collection into batches of the specified size when the total count is evenly divisible by the batch size, and that all batches except possibly the last contain exactly that many items.
**Parameters:** None.
**Returns:** `void`.
**Throws:** Does not throw; the method under test must not throw.

### Batch_WithRemainder_LastBatchHasFewerItems
**Purpose:** Verifies that when the collection size is not evenly divisible by the batch size, the final batch contains the remaining elements (fewer than the batch size).
**Parameters:** None.
**Returns:** `void`.
**Throws:** Does not throw; the method under test must not throw.

### Batch_ZeroBatchSize_ThrowsArgumentException
**Purpose:** Ensures that calling `Batch` with a batch size of zero throws an `ArgumentException`.
**Parameters:** None.
**Returns:** `void`.
**Throws:** The test expects the method under test to throw `ArgumentException`.

### Batch_NullCollection_ThrowsArgumentNullException
**Purpose:** Ensures that calling `Batch` on a `null` collection throws an `ArgumentNullException`.
**Parameters:** None.
**Returns:** `void`.
**Throws:** The test expects the method under test to throw `ArgumentNullException`.

### Batch_EmptyCollection_ReturnsEmptyList
**Purpose:** Verifies that calling `Batch` on an empty collection returns an empty sequence of batches (no batches produced).
**Parameters:** None.
**Returns:** `void`.
**Throws:** Does not throw; the method under test must not throw.

### Partition_SplitsIntoMatchingAndNotMatching
**Purpose:** Verifies that `Partition` splits a collection into two lists: one containing elements that satisfy the predicate, and another containing those that do not.
**Parameters:** None.
**Returns:** `void`.
**Throws:** Does not throw.

### Partition_AllMatch_NotMatchingIsEmpty
**Purpose:** Verifies that when all elements satisfy the predicate, the “not matching” output list is empty.
**Parameters:** None.
**Returns:** `void`.
**Throws:** Does not throw.

### Partition_NoneMatch_MatchingIsEmpty
**Purpose:** Verifies that when no elements satisfy the predicate, the “matching” output list is empty.
**Parameters:** None.
**Returns:** `void`.
**Throws:** Does not throw.

### Partition_NullCollection_ReturnsTwoEmptyLists
**Purpose:** Verifies that calling `Partition` on a `null` collection gracefully returns two empty lists rather than throwing.
**Parameters:** None.
**Returns:** `void`.
**Throws:** Does not throw; the method under test must not throw.

### RemoveWhere_MatchingPredicate_RemovesItems
**Purpose:** Verifies that `RemoveWhere` removes all elements from the list that satisfy the given predicate.
**Parameters:** None.
**Returns:** `void`.
**Throws:** Does not throw.

### RemoveWhere_NoMatches_LeavesListUnchanged
**Purpose:** Verifies that `RemoveWhere` makes no modifications to the list when no elements satisfy the predicate.
**Parameters:** None.
**Returns:** `void`.
**Throws:** Does not throw.

### AddRangeIfNotExists_NewItems_AddsAll
**Purpose:** Verifies that `AddRangeIfNotExists` adds all items from the source collection that are not already present in the target list.
**Parameters:** None.
**Returns:** `void`.
**Throws:** Does not throw.

### AddRangeIfNotExists_DuplicateItems_SkipsDuplicates
**Purpose:** Verifies that `AddRangeIfNotExists` skips items that already exist in the target list, avoiding duplicates.
**Parameters:** None.
**Returns:** `void`.
**Throws:** Does not throw.

## Usage

Below are two realistic examples demonstrating how the extension methods validated by these tests are used in application code.

### Example 1: Safe Indexed Access with Fallback

```csharp
IList<string> items = new List<string> { "alpha", "beta", "gamma" };

// SafeGet prevents IndexOutOfRangeException and handles null lists.
string third = items.SafeGet(2, "fallback");   // "gamma"
string missing = items.SafeGet(5, "fallback"); // "fallback"
string negative = items.SafeGet(-1, "none");   // "none"

IList<string> nullList = null;
string fromNull = nullList.SafeGet(0, "default"); // "default"
```

### Example 2: Batching and Partitioning for Bulk Operations

```csharp
IEnumerable<int> ids = Enumerable.Range(1, 100);

// Batch into groups of 20 for paginated API calls.
foreach (var batch in ids.Batch(20))
{
    // Each batch is an IEnumerable<int> of up to 20 items.
    UploadBatch(batch);
}

// Partition a list of file paths into existing and missing.
IList<string> paths = GetRequestedPaths();
var (existing, missing) = paths.Partition(File.Exists);

ProcessExisting(existing);
ReportMissing(missing);
```

## Notes

- **Null handling:** Methods like `SafeGet`, `IsNullOrEmpty`, `Partition`, and `Batch` are explicitly designed to tolerate `null` inputs. `SafeGet` returns a caller-specified default; `Partition` returns two empty lists; `Batch` throws `ArgumentNullException` to align with standard guard conventions.
- **Edge cases for batching:** A batch size of zero is treated as invalid and throws `ArgumentException`. An empty source collection yields an empty result set, not an exception.
- **Duplicate detection in `AddRangeIfNotExists`:** The method relies on the list’s existing equality semantics (default `Equals` or an overridden implementation). Items already present are silently skipped; no exception is thrown for duplicates.
- **Thread safety:** These extension methods are static utility functions with no shared mutable state. They are safe to call concurrently provided the underlying collection is not modified during enumeration. They do not introduce any internal locking or synchronization.
- **`RemoveWhere` mutability:** This method modifies the list in place. Callers should ensure that the list is not being read or written concurrently from another thread without external synchronization.

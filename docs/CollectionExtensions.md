# CollectionExtensions

Provides a set of static extension methods for collections, lists, and enumerable sequences. These utilities simplify common operations such as safe element retrieval, emptiness checks, batching, partitioning, set intersection, conditional removal, duplicate-aware insertion, and random reordering.

## API

### SafeGet\<T\>

```csharp
public static T SafeGet<T>(this IList<T> list, int index)
```

Returns the element at the specified `index` without throwing an `ArgumentOutOfRangeException`. If the index is outside the valid range of the list, the default value for `T` is returned (`null` for reference types, zero for numeric types, etc.).

**Parameters**
- `list` — the source list.
- `index` — the zero-based position to retrieve.

**Returns**
The element at `index`, or `default(T)` if the index is invalid.

**Throws**
`ArgumentNullException` when `list` is `null`.

---

### IsNullOrEmpty\<T\>

```csharp
public static bool IsNullOrEmpty<T>(this IEnumerable<T> source)
```

Determines whether a sequence is `null` or contains no elements. Evaluates emptiness without enumerating more than one element.

**Parameters**
- `source` — the sequence to test.

**Returns**
`true` if `source` is `null` or empty; otherwise `false`.

**Throws**
Does not throw.

---

### FirstOrDefault\<T\>

```csharp
public static T FirstOrDefault<T>(this IList<T> list)
```

Returns the first element of a list, or the default value for `T` if the list is `null` or empty. This overload operates directly on `IList<T>` and avoids the enumeration overhead of the standard LINQ `FirstOrDefault`.

**Parameters**
- `list` — the source list.

**Returns**
The first element, or `default(T)`.

**Throws**
Does not throw.

---

### Batch\<T\>

```csharp
public static List<List<T>> Batch<T>(this IEnumerable<T> source, int batchSize)
```

Splits a sequence into a list of sub-lists, each containing at most `batchSize` elements. The final batch may be smaller if the total count is not evenly divisible.

**Parameters**
- `source` — the sequence to partition.
- `batchSize` — the maximum number of elements per batch. Must be greater than zero.

**Returns**
A `List<List<T>>` where each inner list represents one batch.

**Throws**
`ArgumentNullException` when `source` is `null`.
`ArgumentOutOfRangeException` when `batchSize` is less than or equal to zero.

---

### Partition\<T\>

```csharp
public static (List<T> matching, List<T> notMatching) Partition<T>(
    this IEnumerable<T> source, Func<T, bool> predicate)
```

Divides a sequence into two lists based on a predicate. The first list contains all elements for which the predicate returns `true`; the second contains those for which it returns `false`.

**Parameters**
- `source` — the sequence to partition.
- `predicate` — the condition used to classify each element.

**Returns**
A tuple with two `List<T>` members: `matching` and `notMatching`.

**Throws**
`ArgumentNullException` when `source` or `predicate` is `null`.

---

### ToTupleList\<K, V\>

```csharp
public static List<(K key, V value)> ToTupleList<K, V>(
    this IDictionary<K, V> dictionary)
```

Converts a dictionary into a list of value tuples, where each tuple holds a key-value pair from the dictionary. Order follows the dictionary's enumerator.

**Parameters**
- `dictionary` — the source dictionary.

**Returns**
A `List<(K key, V value)>` containing all entries.

**Throws**
`ArgumentNullException` when `dictionary` is `null`.

---

### IntersectAll\<T\>

```csharp
public static List<T> IntersectAll<T>(
    this IEnumerable<IEnumerable<T>> collections)
```

Computes the set intersection across multiple sequences. Returns a list of elements that are present in every supplied collection. Uses the default equality comparer for `T`.

**Parameters**
- `collections` — a sequence of collections to intersect.

**Returns**
A `List<T>` containing the common elements.

**Throws**
`ArgumentNullException` when `collections` is `null`.

---

### RemoveWhere\<T\>

```csharp
public static void RemoveWhere<T>(
    this ICollection<T> collection, Func<T, bool> predicate)
```

Removes all elements from a collection that satisfy the given predicate. Modifies the collection in place.

**Parameters**
- `collection` — the mutable collection to modify.
- `predicate` — the condition that identifies elements to remove.

**Throws**
`ArgumentNullException` when `collection` or `predicate` is `null`.

---

### AddRangeIfNotExists\<T\>

```csharp
public static void AddRangeIfNotExists<T>(
    this ICollection<T> collection, IEnumerable<T> items)
```

Adds items to a collection only if they are not already present. Duplicate detection uses the default equality comparer for `T`. Existing elements in the collection are left untouched.

**Parameters**
- `collection` — the target collection.
- `items` — the sequence of candidate items to add.

**Throws**
`ArgumentNullException` when `collection` or `items` is `null`.

---

### Shuffle\<T\>

```csharp
public static List<T> Shuffle<T>(this IEnumerable<T> source)
```

Returns a new list containing all elements from the source sequence in a randomised order. The original sequence is not modified. Uses a cryptographically non-strong random number generator suitable for general-purpose shuffling.

**Parameters**
- `source` — the sequence to shuffle.

**Returns**
A new `List<T>` with elements randomly reordered.

**Throws**
`ArgumentNullException` when `source` is `null`.

## Usage

### Example 1: Batching and Partitioning Log Entries

```csharp
var logEntries = Enumerable.Range(1, 1000)
    .Select(i => new LogEntry { Id = i, Severity = i % 3 == 0 ? "Error" : "Info" })
    .ToList();

// Process in batches of 100 to avoid memory pressure.
foreach (var batch in logEntries.Batch(100))
{
    var (errors, infos) = batch.Partition(e => e.Severity == "Error");
    Console.WriteLine($"Batch: {errors.Count} errors, {infos.Count} infos");
}
```

### Example 2: Deduplicated Insertion and Safe Retrieval

```csharp
var activeIds = new List<int> { 101, 102, 103 };
var incomingIds = new[] { 102, 104, 105, 102 };

activeIds.AddRangeIfNotExists(incomingIds);
// activeIds now contains [101, 102, 103, 104, 105]

var fifthElement = activeIds.SafeGet(5);   // returns 0 (default int)
var firstElement = activeIds.FirstOrDefault(); // returns 101
```

## Notes

- **Null handling**: Methods that accept `IEnumerable<T>`, `IList<T>`, `ICollection<T>`, or `IDictionary<K,V>` throw `ArgumentNullException` when the primary source argument is `null`, unless explicitly documented otherwise (`IsNullOrEmpty` and `FirstOrDefault` tolerate `null`).
- **Empty collections**: `SafeGet` returns `default(T)` for any out-of-range index, including negative indices and indices equal to or greater than the list count. `Batch` returns an empty outer list when the source is empty. `IntersectAll` returns an empty list when any constituent collection is empty or when the outer sequence is empty.
- **Deferred execution**: `Batch`, `Partition`, `Shuffle`, and `IntersectAll` eagerly enumerate the source and allocate new lists. They do not yield deferred iterators.
- **Thread safety**: These methods are static utility functions with no shared state. They are safe to call concurrently provided the caller ensures that no other thread mutates the source collection during execution. `RemoveWhere` and `AddRangeIfNotExists` mutate the passed collection directly and are not safe for concurrent use on the same collection instance without external synchronisation.
- **Equality comparisons**: `AddRangeIfNotExists` and `IntersectAll` rely on `EqualityComparer<T>.Default`. For custom types, ensure that `Equals` and `GetHashCode` are overridden appropriately, or consider providing an overload with an `IEqualityComparer<T>` if needed.
- **Shuffle randomness**: The random number generator is `System.Random`, which is not suitable for cryptographic purposes. Do not use `Shuffle` for security-sensitive randomisation.

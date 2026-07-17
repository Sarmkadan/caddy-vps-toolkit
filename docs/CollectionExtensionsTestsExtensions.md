# CollectionExtensionsTestsExtensions

Extension methods for testing collections in C#. These methods provide convenient, test-focused alternatives to LINQ methods, allowing for more explicit assertions and simplified test code when working with collections in unit tests.

## API

### `FirstOrDefault<T>`
Returns the first element of a sequence, or a default value if the sequence contains no elements.

- **Parameters**
  - `IEnumerable<T> source`: The sequence to return the first element from.
- **Return value**
  - `T`: The first element in the sequence, or `default(T)` if the sequence is empty.
- **Exceptions**
  - Throws `ArgumentNullException` if `source` is `null`.

### `LastOrDefault<T>`
Returns the last element of a sequence, or a default value if the sequence contains no elements.

- **Parameters**
  - `IEnumerable<T> source`: The sequence to return the last element from.
- **Return value**
  - `T`: The last element in the sequence, or `default(T)` if the sequence is empty.
- **Exceptions**
  - Throws `ArgumentNullException` if `source` is `null`.

### `ElementAtOrDefault<T>`
Returns the element at a specified index in a sequence, or a default value if the index is out of range.

- **Parameters**
  - `IEnumerable<T> source`: The sequence to return the element from.
  - `int index`: The zero-based index of the element to retrieve.
- **Return value**
  - `T`: The element at the specified position, or `default(T)` if the index is outside the bounds of the sequence.
- **Exceptions**
  - Throws `ArgumentNullException` if `source` is `null`.
  - Throws `ArgumentOutOfRangeException` if `index` is negative.

### `Distinct<T>`
Returns distinct elements from a sequence by using the default equality comparer to compare values.

- **Parameters**
  - `IEnumerable<T> source`: The sequence to remove duplicates from.
- **Return value**
  - `IEnumerable<T>`: An unordered sequence of distinct elements.
- **Exceptions**
  - Throws `ArgumentNullException` if `source` is `null`.

### `Where<T>`
Filters a sequence of values based on a predicate.

- **Parameters**
  - `IEnumerable<T> source`: The sequence to filter.
  - `Func<T, bool> predicate`: A function to test each element for a condition.
- **Return value**
  - `IEnumerable<T>`: A sequence that contains elements from the input sequence that satisfy the condition.
- **Exceptions**
  - Throws `ArgumentNullException` if `source` or `predicate` is `null`.

### `Select<TSource, TResult>`
Projects each element of a sequence into a new form.

- **Parameters**
  - `IEnumerable<TSource> source`: A sequence of values to invoke a transform function on.
  - `Func<TSource, TResult> selector`: A transform function to apply to each element.
- **Return value**
  - `IEnumerable<TResult>`: A sequence whose elements are the result of invoking the transform function on each element of source.
- **Exceptions**
  - Throws `ArgumentNullException` if `source` or `selector` is `null`.

## Usage

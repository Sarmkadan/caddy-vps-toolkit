# PaginationHelper

A utility class that provides composable, type-safe pagination, filtering, and sorting operations for in-memory collections. It is designed to simplify the construction of paginated queries while maintaining a fluent, builder-style API.

## API

### `Paginate<T>`

Initiates a new paginated query for a collection of type `T`.

- **Parameters**:
  - `source`: `IEnumerable<T>` – The collection to paginate.
- **Return value**: `QueryBuilder<T>` – A fluent builder for chaining operations.
- **Throws**: `ArgumentNullException` if `source` is `null`.

---

### `SortBy<T>`

Applies an in-memory sort to a collection of type `T`.

- **Parameters**:
  - `source`: `IEnumerable<T>` – The collection to sort.
  - `keySelector`: `Func<T, object>` – A function to extract the sort key.
  - `descending`: `bool` – If `true`, sorts in descending order; otherwise ascending.
- **Return value**: `List<T>` – A new sorted list.
- **Throws**: `ArgumentNullException` if `source` or `keySelector` is `null`.

---

### `FilterBy<T>`

Applies a predicate to filter a collection of type `T`.

- **Parameters**:
  - `source`: `IEnumerable<T>` – The collection to filter.
  - `predicate`: `Func<T, bool>` – The filter condition.
- **Return value**: `List<T>` – A new filtered list.
- **Throws**: `ArgumentNullException` if `source` or `predicate` is `null`.

---

### `Filter<T>`

Alias for `FilterBy<T>` for concise filtering.

- **Parameters**: Same as `FilterBy<T>`.
- **Return value**: Same as `FilterBy<T>`.
- **Throws**: Same as `FilterBy<T>`.

---

### `QueryBuilder`

The base builder type returned by `Paginate<T>`.

---

### `QueryBuilder<T>`

A fluent builder for composing pagination, sorting, and filtering operations.

#### `Page`

Sets the page number (1-based).

- **Parameters**:
  - `page`: `int` – The page number.
- **Return value**: `QueryBuilder<T>` – The builder for chaining.
- **Throws**: `ArgumentOutOfRangeException` if `page` is less than 1.

#### `PageSize`

Sets the number of items per page.

- **Parameters**:
  - `size`: `int` – The number of items per page.
- **Return value**: `QueryBuilder<T>` – The builder for chaining.
- **Throws**: `ArgumentOutOfRangeException` if `size` is less than 1.

#### `SortBy`

Applies a sort operation to the query.

- **Parameters**:
  - `keySelector`: `Func<T, object>` – A function to extract the sort key.
  - `descending`: `bool` – If `true`, sorts in descending order; otherwise ascending.
- **Return value**: `QueryBuilder<T>` – The builder for chaining.
- **Throws**: `ArgumentNullException` if `keySelector` is `null`.

#### `Where`

Applies a filter predicate to the query.

- **Parameters**:
  - `predicate`: `Func<T, bool>` – The filter condition.
- **Return value**: `QueryBuilder<T>` – The builder for chaining.
- **Throws**: `ArgumentNullException` if `predicate` is `null`.

#### `Execute`

Executes the composed query and returns a paginated result.

- **Return value**: `PaginatedResult<T>` – The paginated result containing the current page of items and total count.
- **Throws**: `InvalidOperationException` if `Page` or `PageSize` has not been set.

#### `ExecuteUnpaged`

Executes the composed query without pagination and returns the full result set.

- **Return value**: `List<T>` – The full list of filtered and sorted items.
- **Throws**: No documented exceptions.

## Usage

### Example 1: Basic Pagination

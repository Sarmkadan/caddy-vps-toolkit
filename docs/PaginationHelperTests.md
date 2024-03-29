# PaginationHelperTests

`PaginationHelperTests` is a test suite that validates the behaviour of the `PaginationHelper` utility within the `caddy-vps-toolkit` project. It provides exhaustive coverage of pagination, sorting, filtering, and combined execution paths, ensuring that edge cases such as null collections, out-of-range page requests, and invalid property names are handled correctly.

## API

### public string Name
A descriptive identifier for the test suite instance. Used primarily for display purposes in test runners and logs.

### public int Value
An integer property associated with the test suite instance. Its semantics are determined by the test context; it is not used internally by the test methods themselves.

### public void Paginate_FirstPage_ReturnsCorrectSlice
Verifies that requesting the first page of a collection returns the expected leading slice of items, respecting the specified page size.

### public void Paginate_LastPage_ReturnsRemainingItems
Confirms that requesting the final page returns all remaining items when the total count is not evenly divisible by the page size.

### public void Paginate_PageBeyondTotal_ReturnsEmptyItems
Ensures that requesting a page number greater than the total number of available pages yields an empty collection rather than throwing an exception.

### public void Paginate_NullCollection_TreatsAsEmpty
Validates that passing a null source collection is handled gracefully, producing an empty paginated result without a null-reference error.

### public void Paginate_PageLessThanOne_ClampsToOne
Demonstrates that a page number less than one is automatically clamped to page one, preventing invalid indexing.

### public void Paginate_PageSizeLessThanOne_ClampsToTen
Shows that a page size less than one is clamped to a default size of ten, ensuring a sensible minimum page size.

### public void SortBy_Ascending_SortsCorrectly
Tests that sorting by a valid property name in ascending order arranges items from smallest to largest according to the property’s comparable value.

### public void SortBy_Descending_SortsCorrectly
Tests that sorting by a valid property name in descending order arranges items from largest to smallest.

### public void SortBy_UnknownProperty_ReturnsUnsortedList
Verifies that specifying a property name that does not exist on the element type returns the list in its original order, without throwing an exception.

### public void SortBy_NullCollection_ReturnsEmptyList
Ensures that attempting to sort a null collection returns an empty list rather than causing a runtime failure.

### public void FilterBy_ExistingPropertyValue_ReturnsMatchingItems
Confirms that filtering by a known property and a specific value returns only those items whose property matches the given value.

### public void Filter_WithPredicate_ReturnsMatchingItems
Validates that a predicate-based filter correctly retains items that satisfy the predicate expression.

### public void Filter_NullCollection_ReturnsEmptyList
Ensures that filtering a null collection safely returns an empty list.

### public void Execute_WithPageAndPageSize_ReturnsPaginatedResult
Tests the combined `Execute` method, verifying that providing both a page number and a page size produces a correctly sliced result set.

### public void Execute_WithWhereFilter_FiltersBeforePagination
Demonstrates that when a `Where` filter is supplied to `Execute`, filtering is applied before the pagination slice, so the page is taken from the already-filtered subset.

### public void ExecuteUnpaged_ReturnsAllFilteredItems
Verifies that calling the unpaged execution variant returns all items that match the filter, without applying any pagination boundaries.

### public void Execute_WithNullData_ReturnsEmptyResult
Ensures that executing with a null data source returns an empty result rather than throwing an exception.

### public void Execute_ChainedWhereFilters_AppliesBoth
Confirms that multiple chained `Where` filters are all applied cumulatively, so only items satisfying every filter appear in the final output.

## Usage

```csharp
// Example 1: Verifying basic pagination clamping behaviour
var tests = new PaginationHelperTests();
tests.Paginate_PageLessThanOne_ClampsToOne();
tests.Paginate_PageSizeLessThanOne_ClampsToTen();
tests.Paginate_PageBeyondTotal_ReturnsEmptyItems();
```

```csharp
// Example 2: Testing a full filter-sort-paginate pipeline
var tests = new PaginationHelperTests();
tests.FilterBy_ExistingPropertyValue_ReturnsMatchingItems();
tests.SortBy_Descending_SortsCorrectly();
tests.Execute_WithWhereFilter_FiltersBeforePagination();
tests.Execute_ChainedWhereFilters_AppliesBoth();
```

## Notes

- All methods that accept a collection treat `null` as an empty collection, preventing `NullReferenceException` throughout the pagination, sorting, and filtering operations.
- Page and page size values less than one are clamped to sensible defaults (page one and size ten, respectively), so callers do not need to normalise these values beforehand.
- Sorting by a property name that does not exist results in the original unsorted list; no exception is raised, making the behaviour safe for dynamically supplied property names.
- When chaining multiple `Where` filters, all predicates are applied in sequence, and only items satisfying every predicate survive into the final result.
- The test methods are synchronous and single-threaded; they do not mutate shared state, so they are safe to run in parallel within a test runner that isolates test instances.

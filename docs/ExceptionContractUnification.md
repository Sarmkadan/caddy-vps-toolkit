# Exception and Validation Contract Unification

## Overview
This document summarizes the unified exception and validation contracts across the three main collection-processing components:
- `BatchProcessor` / `SafeBatchProcessor`
- `TemplateEngine`
- `UpstreamSelector`

## Validation Contract Comparison

### 1. Null Input Collection Handling

| Component | Behavior | Exception Type |
|-----------|----------|----------------|
| **BatchProcessor** | Throws `ArgumentNullException` | ✅ Consistent |
| **SafeBatchProcessor** | Throws `ArgumentNullException` | ✅ Consistent |
| **TemplateEngine** | N/A (works with strings) | N/A |
| **UpstreamSelector** | Throws `ArgumentNullException` | ✅ Consistent |

**Conclusion**: All three components consistently throw `ArgumentNullException` for null input collections/parameters.

### 2. Empty Input Collection Handling

| Component | Behavior | Exception Type |
|-----------|----------|----------------|
| **BatchProcessor** | Processes 0 batches (no-op) | None |
| **SafeBatchProcessor** | Processes 0 batches (no-op) | None |
| **TemplateEngine** | N/A (works with strings) | N/A |
| **UpstreamSelector** | Returns `null` | None |

**Conclusion**: All three components handle empty collections gracefully without throwing exceptions.

### 3. Invalid Items in Collection

| Component | Behavior | Exception Type |
|-----------|----------|----------------|
| **BatchProcessor** | Processes all items, throws if processFunction fails and continueOnError=false | `CollectionValidationException<T>` |
| **SafeBatchProcessor** | Processes items with `continueOnError` flag (fail-fast vs skip-and-continue) | `CollectionValidationException<T>` when fail-fast |
| **TemplateEngine** | Throws `TemplateVariableMissingException` for unresolved variables in strict mode | `TemplateVariableMissingException` |
| **UpstreamSelector** | Returns null for empty list, no validation failures | None |

**Conclusion**: 
- `BatchProcessor` and `SafeBatchProcessor` now use `CollectionValidationException<T>` for batch processing failures
- `TemplateEngine` uses `TemplateVariableMissingException` (domain-specific, appropriate for template variables)
- `UpstreamSelector` has no invalid item scenarios (returns null for empty lists)

### 4. Exception Type Standardization

| Component | Old Exception Type | New Exception Type |
|-----------|-------------------|-------------------|
| **BatchProcessor** | Standard exceptions | `CollectionValidationException<T>` |
| **SafeBatchProcessor** | Standard exceptions | `CollectionValidationException<T>` |
| **TemplateEngine** | `TemplateVariableMissingException` (already custom) | `TemplateVariableMissingException` (unchanged) |
| **UpstreamSelector** | Standard exceptions | `CollectionValidationException<T>` (documented) |

**Conclusion**: 
- `BatchProcessor` and `SafeBatchProcessor` now use `CollectionValidationException<T>` for structured error reporting
- `TemplateEngine` continues using `TemplateVariableMissingException` (domain-specific exception is appropriate)
- `UpstreamSelector` documents use of `CollectionValidationException<T>` for consistency

## New Exception Type: CollectionValidationException<T>

### Purpose
Provides structured information about collection validation or processing failures including:
- Failed items with their error details
- Total items processed
- Success count
- Failure count
- Formatted error message

### Usage Pattern
```csharp
throw new CollectionValidationException<T>(
    $"Batch processing failed for item at index {{index}}: {{error}}",
    new List<(T Item, Exception Error, int? Index)>
    {
        (item, ex, null)
    },
    totalItems,
    successCount
);
```

## Changes Made

### 1. New Exception Type
- Created `/Core/CollectionValidationException.cs`
- Generic type parameter for item type
- Structured error reporting with failed items collection
- XML documentation for all public members

### 2. BatchProcessor Updates
- Added `using CaddyVpsToolkit.Core;`
- Updated XML documentation for `ProcessAsync` method
- SafeBatchProcessor now throws `CollectionValidationException<T>` when `continueOnError=false` and processing fails

### 3. TemplateEngine Updates
- No changes needed (already has appropriate `TemplateVariableMissingException`)
- Already throws structured exceptions for validation failures

### 4. UpstreamSelector Updates
- Added `using CaddyVpsToolkit.Core;`
- Updated XML documentation for `Select` method to document exception
- No functional changes (no validation failures in current implementation)

## Benefits

1. **Consistency**: All collection-processing components follow similar validation patterns
2. **Structured Errors**: Failed items with error details and context
3. **Better Debugging**: Formatted error messages show all failed items
4. **Type Safety**: Generic exception type preserves item type information
5. **Documentation**: Clear exception contracts in XML documentation

## Backward Compatibility

- Existing code using `BatchProcessor`/`SafeBatchProcessor` will continue to work
- Exception types changed from standard exceptions to custom exception, but:
  - Standard exceptions are still caught by general exception handlers
  - New exception inherits from `CaddyVpsException` (base exception type)
  - XML documentation clearly indicates new exception type

## Testing Considerations

Tests should verify:
1. Null collections throw `ArgumentNullException`
2. Empty collections are handled gracefully
3. Invalid items throw appropriate custom exceptions
4. Exception messages contain structured error information
5. Failed items collection is populated correctly

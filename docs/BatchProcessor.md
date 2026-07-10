# BatchProcessor

A utility class for processing batches of items with built-in tracking of successful and failed operations. Designed for scenarios where individual items in a collection may succeed or fail independently, allowing for partial success reporting and error aggregation.

## API

### `BatchProcessor`
The primary constructor for the `BatchProcessor` class. Initializes a new instance ready to process batches of items of type `T`.

### `public async Task ProcessAsync`
Processes a batch of items asynchronously. Each item is processed according to the provided delegate, and results are tracked internally.

**Parameters:**
- `items` (`IEnumerable<T>`): The collection of items to process.
- `processItemAsync` (`Func<T, Task>`): An asynchronous delegate that defines the processing logic for each item.

**Returns:**
A `Task` representing the asynchronous operation.

**Throws:**
- `ArgumentNullException`: Thrown if `items` or `processItemAsync` is `null`.

---

### `public List<T> SuccessfulItems`
Gets the list of items that were processed successfully during the last execution of `ProcessAsync`.

**Returns:**
A `List<T>` containing all items that completed without throwing an exception.

---

### `public List<(T item, Exception error)> FailedItems`
Gets the list of items that failed during the last execution of `ProcessAsync`, along with the associated exception for each failure.

**Returns:**
A `List<(T item, Exception error)>` containing tuples of failed items and their corresponding exceptions.

---

### `public string GetReport`
Generates a human-readable report summarizing the results of the last batch processing operation, including counts of successful and failed items.

**Returns:**
A `string` containing the formatted report.

---

### `SafeBatchProcessor`
A nested type derived from `BatchProcessor` that ensures exceptions during item processing do not halt the entire batch. Instead, failures are captured and recorded in `FailedItems`.

---

### `public async Task<BatchResult<T>> ProcessAsync`
An overloaded method available in `SafeBatchProcessor` that processes a batch of items and returns a structured result object.

**Parameters:**
- `items` (`IEnumerable<T>`): The collection of items to process.
- `processItemAsync` (`Func<T, Task>`): An asynchronous delegate that defines the processing logic for each item.

**Returns:**
A `Task<BatchResult<T>>` containing the outcome of the batch processing operation, including successful items, failed items, and any aggregated errors.

**Throws:**
- `ArgumentNullException`: Thrown if `items` or `processItemAsync` is `null`.

## Usage

### Example 1: Basic Batch Processing

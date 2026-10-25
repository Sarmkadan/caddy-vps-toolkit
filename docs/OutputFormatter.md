# OutputFormatter

`Formatters/OutputFormatter.cs` contains four public formatter classes in the `CaddyVpsToolkit.Formatters` namespace. Each one renders a list or a single item as a `string` for console output:

- [`TableFormatter`](#tableformatter): aligned, fixed-width columns.
- [`CsvFormatter`](#csvformatter): comma-separated values with RFC-style quoting.
- [`JsonFormatter`](#jsonformatter): indented JSON through `System.Text.Json`.
- [`TextFormatter`](#textformatter): `Name: value` lines, one block per item.

The file is named `OutputFormatter.cs`, but no type named `OutputFormatter` exists in it. All four classes are `public sealed` and implement `IOutputFormatter`, which declares `Format<T>(List<T> items)` and `Format<T>(T item)`. Every class has the same two overloads. Pass a `List<T>` to get the list overload; any other type, including arrays and `IEnumerable<T>`, goes to the single-item overload.

To register these classes with dependency injection, see [FormatterServiceCollectionExtensions](FormatterServiceCollectionExtensions.md).

## API

### `TableFormatter`
```csharp
public sealed class TableFormatter : IOutputFormatter
public TableFormatter(params string[] headers)
```

Renders rows as fixed-width columns. The constructor takes the column headers. Row values come from the public properties of `T`, in the order `typeof(T).GetProperties()` returns them, so the headers must be listed in the same order as the properties.

**Parameters (constructor):**
- `headers` (`string[]`): The column titles, in property order.

#### `Format<T>(List<T> items)`
```csharp
public string Format<T>(List<T> items)
```

Returns a header row, a separator line of dashes, and one row per item.

- Each cell is padded with `PadRight(20)`. The separator is `headers.Length * 20` dashes long.
- Null property values are written as empty cells.
- If `items` is `null` or empty, returns `"No items"`.

**Return value:** The table as one string, with lines joined by `Environment.NewLine`.

#### `Format<T>(T item)`
```csharp
public string Format<T>(T item)
```

Returns one padded row for `item`. It does not write the header or the separator.

- If `item` is `null`, returns `"No item"`.

**Return value:** A single row string.

### `CsvFormatter`
```csharp
public sealed class CsvFormatter : IOutputFormatter
```

Writes a header row built from the property names of `T`, then one row per item. The constructor takes no arguments and holds no state.

#### `Format<T>(List<T> items)`
```csharp
public string Format<T>(List<T> items)
```

- Header names come from `typeof(T).GetProperties()`, not from a constructor argument.
- A field is wrapped in double quotes when it contains a comma, a double quote, or a `\n`. Inner double quotes are doubled.
- Null property values are written as empty fields.
- If `items` is `null` or empty, returns an empty string. No header is written.

**Return value:** The CSV text, with lines joined by `Environment.NewLine`.

#### `Format<T>(T item)`
```csharp
public string Format<T>(T item)
```

Wraps `item` in a one-element list and calls the list overload, so the result includes the header row.

- If `item` is `null`, returns an empty string.

**Return value:** CSV text with a header row and one data row.

### `JsonFormatter`
```csharp
public sealed class JsonFormatter : IOutputFormatter
```

Serializes with `JsonSerializer.Serialize` using `WriteIndented = true` and otherwise default options. Property names are written as declared, so no camelCase conversion is applied.

#### `Format<T>(List<T> items)`
```csharp
public string Format<T>(List<T> items)
```

- If `items` is `null`, it is treated as an empty list and returns `"[]"`.

**Return value:** An indented JSON array.

#### `Format<T>(T item)`
```csharp
public string Format<T>(T item)
```

- If `item` is `null`, returns the string `"null"`.

**Return value:** An indented JSON object or value.

**Exceptions (both overloads):** Errors from `JsonSerializer` propagate unchanged. For example, a cyclic object graph throws `JsonException`.

### `TextFormatter`
```csharp
public sealed class TextFormatter : IOutputFormatter
```

Writes one `Name: value` line for each public property. Null values are written as `(null)`.

#### `Format<T>(List<T> items)`
```csharp
public string Format<T>(List<T> items)
```

- Each item starts with a `--- Item N ---` line, numbered from 1, and is followed by a blank line.
- If `items` is `null` or empty, returns `"No items"`.

**Return value:** The formatted blocks, with lines joined by `Environment.NewLine`.

#### `Format<T>(T item)`
```csharp
public string Format<T>(T item)
```

- Writes only the `Name: value` lines, without an item header or a trailing blank line.
- If `item` is `null`, returns `"No item"`.

**Return value:** The property lines for `item`.

---

## Usage

### Example 1: Printing a table of services
```csharp
using CaddyVpsToolkit.Formatters;

// Declared at namespace level; its property order matches the headers
public sealed record ServiceRow(string Name, string Status);

var table = new TableFormatter("Name", "Status");
string output = table.Format(new List<ServiceRow>
{
    new("web", "running"),
    new("api", "stopped")
});

Console.WriteLine(output);
// Name                Status
// ----------------------------------------
// web                 running
// api                 stopped
```

### Example 2: Exporting rows as CSV
```csharp
using CaddyVpsToolkit.Formatters;

var csv = new CsvFormatter();
var rows = new List<ServiceRow>
{
    new("a,b", "say \"hi\"")
};

string output = csv.Format(rows);
// Name,Status
// "a,b","say ""hi"""

string empty = csv.Format(new List<ServiceRow>());
// ""  (no header when there are no items)
```

### Example 3: Returning JSON from a single item
```csharp
using CaddyVpsToolkit.Formatters;

var json = new JsonFormatter();
string output = json.Format(new List<string> { "example.com", "api.example.com" });
// [
//   "example.com",
//   "api.example.com"
// ]

string nothing = json.Format((string)null!);
// "null"
```

### Example 4: Showing one item as text
```csharp
using CaddyVpsToolkit.Formatters;

// Declared at namespace level
public sealed class Note
{
    public string? Title { get; set; }
    public int Count { get; set; }
}

var text = new TextFormatter();
string output = text.Format(new Note { Title = null, Count = 3 });
// Title: (null)
// Count: 3
```

### Example 5: Choosing a formatter at runtime
Because all four classes implement `IOutputFormatter`, a caller can pick one by name and use it through the interface.
```csharp
using CaddyVpsToolkit.Formatters;

IOutputFormatter formatter = outputName switch
{
    "csv" => new CsvFormatter(),
    "json" => new JsonFormatter(),
    "text" => new TextFormatter(),
    _ => new TableFormatter("Name", "Status")
};

string output = formatter.Format(rows);
```

---

## Notes

- **Overload ambiguity with `null`**: A `null` argument does not compile, because both overloads accept it. `Format<ServiceRow>(null!)` and `Format(null!)` are both ambiguous. Cast the null to the type you mean, such as `Format((ServiceRow)null!)` for an item or `Format((List<ServiceRow>)null!)` for a list.
- **Table column width**: Cells are padded to 20 characters but never truncated. A longer value pushes the rest of its row out of alignment. The constructor also fills an internal width array, but formatting does not use it.
- **Header and property mismatch**: `TableFormatter` does not check that the header count matches the number of properties. Keep the headers and the property order in step.
- **Null entries inside a list**: The list overloads do not check individual items. Do not pass `null` elements.
- **Empty CSV**: An empty list gives an empty string, so a CSV file written from it has no header.
- **Thread safety**: All four classes hold only read-only state after construction, so one instance can be shared across threads.
- **Scope**: The formatters only build strings. They do not write to the console, a file, or a stream.

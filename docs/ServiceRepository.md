# ServiceRepository

A repository class that provides asynchronous access to managed services in the Caddy VPS toolkit, encapsulating operations for querying, adding, updating, and deleting services with type-safe methods.

## API

### `ServiceRepository`
The repository class for managing `ManagedService` entities. All operations are asynchronous and thread-safe by design.

### `public async Task<ManagedService> GetByIdAsync(int id)`
Retrieves a managed service by its unique identifier.

- **Parameters**:
  - `id` (int): The unique identifier of the service.
- **Returns**: A `Task<ManagedService>` representing the asynchronous operation. The task result contains the service if found; otherwise, `null`.
- **Throws**: `ArgumentOutOfRangeException` if `id` is less than or equal to zero.

### `public async Task<ManagedService> GetByNameAsync(string name)`
Retrieves a managed service by its name.

- **Parameters**:
  - `name` (string): The name of the service, case-sensitive.
- **Returns**: A `Task<ManagedService>` representing the asynchronous operation. The task result contains the service if found; otherwise, `null`.
- **Throws**: `ArgumentException` if `name` is `null` or whitespace.

### `public async Task<List<ManagedService>> GetAllAsync()`
Retrieves all managed services.

- **Returns**: A `Task<List<ManagedService>>` representing the asynchronous operation. The task result contains a list of all services, which may be empty.
- **Throws**: No exceptions.

### `public async Task<List<ManagedService>> GetByTypeAsync(string type)`
Retrieves all managed services of a specific type.

- **Parameters**:
  - `type` (string): The type of the service to filter by, case-sensitive.
- **Returns**: A `Task<List<ManagedService>>` representing the asynchronous operation. The task result contains a list of matching services, which may be empty.
- **Throws**: `ArgumentException` if `type` is `null` or whitespace.

### `public async Task<List<ManagedService>> GetEnabledServicesAsync()`
Retrieves all enabled managed services.

- **Returns**: A `Task<List<ManagedService>>` representing the asynchronous operation. The task result contains a list of enabled services, which may be empty.
- **Throws**: No exceptions.

### `public async Task<string> AddAsync(ManagedService service)`
Adds a new managed service to the repository.

- **Parameters**:
  - `service` (ManagedService): The service to add.
- **Returns**: A `Task<string>` representing the asynchronous operation. The task result contains a unique identifier for the added service.
- **Throws**:
  - `ArgumentNullException` if `service` is `null`.
  - `InvalidOperationException` if a service with the same name already exists.

### `public async Task<bool> UpdateAsync(ManagedService service)`
Updates an existing managed service in the repository.

- **Parameters**:
  - `service` (ManagedService): The service with updated properties.
- **Returns**: A `Task<bool>` representing the asynchronous operation. The task result is `true` if the service was found and updated; otherwise, `false`.
- **Throws**:
  - `ArgumentNullException` if `service` is `null`.
  - `ArgumentOutOfRangeException` if `service.Id` is less than or equal to zero.

### `public async Task<bool> DeleteAsync(int id)`
Deletes a managed service by its unique identifier.

- **Parameters**:
  - `id` (int): The unique identifier of the service to delete.
- **Returns**: A `Task<bool>` representing the asynchronous operation. The task result is `true` if the service was found and deleted; otherwise, `false`.
- **Throws**: `ArgumentOutOfRangeException` if `id` is less than or equal to zero.

### `public async Task<bool> ExistsAsync(int id)`
Checks whether a managed service with the specified identifier exists.

- **Parameters**:
  - `id` (int): The unique identifier of the service.
- **Returns**: A `Task<bool>` representing the asynchronous operation. The task result is `true` if the service exists; otherwise, `false`.
- **Throws**: `ArgumentOutOfRangeException` if `id` is less than or equal to zero.

### `public async Task<int> GetCountAsync()`
Retrieves the total number of managed services.

- **Returns**: A `Task<int>` representing the asynchronous operation. The task result contains the count of services.
- **Throws**: No exceptions.

### `public async Task<List<ManagedService>> SearchAsync(string query)`
Searches managed services by a query string, matching against name or type.

- **Parameters**:
  - `query` (string): The search term, case-insensitive.
- **Returns**: A `Task<List<ManagedService>>` representing the asynchronous operation. The task result contains a list of matching services, which may be empty.
- **Throws**: `ArgumentException` if `query` is `null` or whitespace.

## Usage

### Example 1: Retrieving and updating a service

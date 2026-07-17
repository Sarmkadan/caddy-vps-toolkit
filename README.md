## ResultExtensions

ResultExtensions is a static class that provides extension methods for working with results.

Example usage:
```csharp
public static Result<TResult> Map<T, TResult>(this Result<T> result, Func<T, TResult> func)
public static Result<TResult> Bind<T, TResult>(this Result<T> result, Func<T, TResult> func)
public static bool ToBoolean<T>(this Result<T> result)
public static bool ToBoolean(this Result result)
public static string? GetErrorOrNull<T>(this Result<T> result)
public static string? GetErrorOrNull(this Result result)
public static void OnSuccess<T>(this Result<T> result, Action<T> action)
public static void OnSuccess(this Result result, Action action)
public static void OnFailure<T>(this Result<T> result, Action action)
public static void OnFailure(this Result result, Action action)
public static IReadOnlyList<T> ToReadOnlyList<T>(this Result<T> result)
public static (bool IsSuccess, T Data) ToTuple<T>(this Result<T> result)
public static (bool Success, string ErrorMessage) ToTuple(this Result result)
```

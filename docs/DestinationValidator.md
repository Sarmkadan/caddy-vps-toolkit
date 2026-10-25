# DestinationValidator

`DestinationValidator` checks destination strings for notification providers before they are used: email addresses, webhook URLs, and phone numbers. It also checks message content for template-injection tokens. Every method either returns the validated (and usually trimmed) value or throws `ArgumentException`. The class is a static helper with no state, so it is safe to call from any thread. The types live in the `CaddyVpsToolkit.Notifications` namespace and are defined in `Notifications/DestinationValidator.cs`.

The public surface consists of:

- `DestinationValidator` - the static validator class.
- `DestinationType` - the enum selecting which validation `ValidateDestination` applies (`Email`, `Webhook`, `Phone`).

`NotificationServiceExtensions` adds `this string` wrappers around these methods (for example `ValidateEmail` and `ValidateDestination`). Those wrappers are documented with the notification service and are not part of this class.

## API

### `enum DestinationType`

The kind of destination a string represents.

| Value | Validated by |
|-------|--------------|
| `Email` | `ValidateEmail` |
| `Webhook` | `ValidateWebhookUrl` |
| `Phone` | `ValidatePhoneNumber` |

---

### `class DestinationValidator`

Static class. It has no constructor and no instance members. The validation rules are compiled regular expressions and checks held in private static fields.

#### `static string ValidateEmail`

```csharp
public static string ValidateEmail(string email, bool allowNullOrEmpty = false)
```

Checks that `email` matches a standard address pattern: a local part of letters, digits, and the characters `.!#$%&'*+/=?^_`{|}~-`, followed by `@` and a domain of dot-separated labels. Each label is 1-63 letters, digits, or hyphens, and cannot start or end with a hyphen.

The format check runs on the value as passed in. Leading or trailing whitespace therefore fails the check. Trimming is applied only to the returned value.

**Parameters:**  
- `email` - the address to validate.  
- `allowNullOrEmpty` - when `true`, `null` or `""` is returned unchanged instead of throwing. Whitespace-only values are still rejected.

**Returns:**  
The validated address, trimmed of surrounding whitespace.

**Throws:**  
- `ArgumentException` if the value is empty or whitespace (and `allowNullOrEmpty` is `false`), or if it does not match the address pattern. The message includes the rejected value.

---

#### `static string ValidateWebhookUrl`

```csharp
public static string ValidateWebhookUrl(string webhookUrl, bool allowNullOrEmpty = false)
```

Checks that `webhookUrl` is an absolute `http` or `https` URL that does not point at the local host or a private network. The value is trimmed before any check.

Checks run in this order:

1. The value must be an absolute URI (`Uri.TryCreate` with `UriKind.Absolute`).
2. The scheme must be `http` or `https`.
3. The host must not be `localhost`, `127.0.0.1`, `::1`, `0.0.0.0`, or anything in `127.0.0.0/8`. Literal IP hosts in `10.0.0.0/8`, `172.16.0.0/12`, `192.168.0.0/16`, `169.254.0.0/16`, and IPv6 link-local, site-local, and unique-local (`fc00::/7`) ranges are also rejected.
4. The host must not be empty.

Hostnames are not resolved. A name such as `hooks.internal.example` that resolves to a private address passes this check. The HTTP client is responsible for any DNS-level protection.

**Parameters:**  
- `webhookUrl` - the URL to validate.  
- `allowNullOrEmpty` - when `true`, `null` or `""` is returned unchanged instead of throwing.

**Returns:**  
The validated URL, trimmed of surrounding whitespace. The URL is otherwise returned as written.

**Throws:**  
- `ArgumentException` if the value is empty or whitespace (and `allowNullOrEmpty` is `false`), is not an absolute URI, uses a scheme other than `http` or `https`, points at a local or private address, or has no host. The message names the rejected value, scheme, or host.

---

#### `static string ValidatePhoneNumber`

```csharp
public static string ValidatePhoneNumber(string phoneNumber, bool allowNullOrEmpty = false)
```

Checks that `phoneNumber` has an optional leading `+` followed by at least eight characters from digits, spaces, `-`, `(`, and `)`. Formats such as `+1234567890` and `123-456-7890` pass. Letters and other symbols fail. No country-code or length-per-country rules are applied.

**Parameters:**  
- `phoneNumber` - the number to validate.  
- `allowNullOrEmpty` - when `true`, `null` or `""` is returned unchanged instead of throwing.

**Returns:**  
The validated number, trimmed of surrounding whitespace. Internal spacing and punctuation are kept as written.

**Throws:**  
- `ArgumentException` if the value is empty or whitespace (and `allowNullOrEmpty` is `false`), or if it does not match the phone pattern. The message includes the rejected value and the expected formats.

---

#### `static string ValidateMessageContent`

```csharp
public static string ValidateMessageContent(string message, bool allowNullOrEmpty = false)
```

Rejects message text that contains template tokens. The check blocks `{{...}}` placeholders and format tokens of the form `%name$`. The intent is that message content is sent literally and never evaluated as a template. The value is trimmed before the check.

**Parameters:**  
- `message` - the message text to validate.  
- `allowNullOrEmpty` - when `true`, `null` or `""` is returned unchanged instead of throwing.

**Returns:**  
The validated message, trimmed of surrounding whitespace.

**Throws:**  
- `ArgumentException` if the value is empty (and `allowNullOrEmpty` is `false`), or if it contains a template injection pattern.

Note that whitespace-only input is not rejected here. It is trimmed to an empty string and returned.

---

#### `static string ValidateDestination`

```csharp
public static string ValidateDestination(
    string destination,
    DestinationType destinationType,
    bool allowNullOrEmpty = false)
```

Dispatches to `ValidateEmail`, `ValidateWebhookUrl`, or `ValidatePhoneNumber` based on `destinationType`. `allowNullOrEmpty` is passed through unchanged.

**Parameters:**  
- `destination` - the value to validate.  
- `destinationType` - the kind of destination.  
- `allowNullOrEmpty` - forwarded to the selected method.

**Returns:**  
The value returned by the selected validator.

**Throws:**  
- `ArgumentException` as described for the selected validator.  
- `ArgumentOutOfRangeException` if `destinationType` is not a defined `DestinationType` value.

---

## Usage

### Validating a single destination

```csharp
using CaddyVpsToolkit.Notifications;

try
{
    string email = DestinationValidator.ValidateEmail("  ops@example.com  ");
    // email == "ops@example.com"
}
catch (ArgumentException ex)
{
    logger.LogWarning(ex, "Rejected notification email");
}
```

### Optional destinations

Pass `allowNullOrEmpty: true` when a destination is optional. The value is returned as given, so `null` stays `null`:

```csharp
string? webhook = DestinationValidator.ValidateWebhookUrl(config.WebhookUrl, allowNullOrEmpty: true);

if (webhook is not null)
{
    // Only register the webhook provider when a URL was configured.
}
```

### Validating by destination type

Use `ValidateDestination` when the type is only known at runtime, for example when reading provider settings:

```csharp
string validated = DestinationValidator.ValidateDestination(
    settings.Destination,
    DestinationType.Phone);
```

### Rejecting private webhook targets

```csharp
// Throws: the host is a loopback address.
DestinationValidator.ValidateWebhookUrl("http://127.0.0.1:8080/hook");

// Throws: the scheme is not http or https.
DestinationValidator.ValidateWebhookUrl("ftp://hooks.example.com/hook");

// Passes: a public hostname. It is not resolved during validation.
DestinationValidator.ValidateWebhookUrl("https://hooks.example.com/notify");
```

### Checking message content before sending

```csharp
// Throws: "{{user}}" would be treated as a template placeholder.
DestinationValidator.ValidateMessageContent("Deploy finished for {{user}}");

// Passes: plain text is sent as written.
string body = DestinationValidator.ValidateMessageContent("Deploy finished for host-01");
```

---

## Notes

- The validator is not a substitute for network-level controls. Hostnames are not resolved, so DNS-based SSRF has to be handled by the HTTP client or by firewall rules.  
- Validation errors are `ArgumentException` with the parameter name set to the argument, so `ex.ParamName` identifies which input failed.  
- The class is static and has no configuration. To change the rules, edit the regular expressions or the private range checks in `Notifications/DestinationValidator.cs`.

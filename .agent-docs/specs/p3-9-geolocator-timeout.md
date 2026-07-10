# Geolocator Timeout and Typed Error

## Problem Statement

At first run, HueShift2 calls the IpStack API to determine the user's location. The current `Geolocator` implementation creates an `HttpClient` inline with no timeout, no error handling, and no typed exception. If the API is slow or unreachable, `LightingConfigFileManager.Generate()` hangs indefinitely with no user-visible feedback. An invalid API key or a malformed response produces a `NullReferenceException` with no actionable message.

## Solution

Inject an `HttpClient` with a configured 10-second timeout. Wrap the call in a `try/catch` that throws a `GeolocationUnavailableException` with a descriptive message on any failure. The exception message must never include the API key.

## User Stories

1. As a user, I want the app to fail fast with a clear error if geolocation is unavailable at startup, so that I know what to fix rather than waiting indefinitely.
2. As a developer, I want a typed `GeolocationUnavailableException`, so that callers can catch and handle geolocation failures distinctly from other startup errors.
3. As a developer, I want the API key excluded from all log messages and exception messages, so that it is not inadvertently exposed.

## Implementation Decisions

### `GeolocationUnavailableException`

New exception class in `HueShift2.Configuration`:

```csharp
public class GeolocationUnavailableException : Exception {
    public GeolocationUnavailableException(string message, Exception inner = null)
        : base(message, inner) { }
}
```

Message must include the URI attempted (scheme + host + path only, no query string or API key) and a suggestion to check the `IpStackApi` configuration section.

### `Geolocator`

- Replace inline `new HttpClient()` with constructor-injected `HttpClient`. Register a named client in DI with `Timeout = TimeSpan.FromSeconds(10)` via `IHttpClientFactory`, or inject a pre-configured `HttpClient` directly.
- Change the URI construction to separate the base URI from the key: build the `HttpRequestMessage` with the full URI internally but never expose the key in log output or exception messages.
- Wrap the HTTP call and JSON parse in `try/catch (Exception e)`. On any failure, throw `GeolocationUnavailableException` with a safe message and the original exception as inner.
- On a successful response, validate that `latitude` and `longitude` are present and numeric before constructing `Geolocation`. Throw `GeolocationUnavailableException` on parse failure.

### `IGeolocationUnavailableException` / caller

`LightingConfigFileManager.FindGeolocation()` currently lets any exception propagate. After this change, `GeolocationUnavailableException` will propagate naturally and be caught at the startup boundary where a clear user message can be logged.

### DI Registration

Register the `HttpClient` for `Geolocator` with a 10-second timeout in `Program.cs` / `Startup.cs` using `IHttpClientFactory` or a named client configuration.

## Testing Decisions

**`GeolocatorTests`** — new test class in `HueShift2.Tests/Configuration/`:

- HTTP timeout (simulated via `HttpMessageHandler` fake) → `GeolocationUnavailableException` thrown within 15 seconds; message excludes the API key.
- Malformed/non-JSON response → `GeolocationUnavailableException` with "parse failure" in message.
- Successful response with valid lat/long → correct `Geolocation` returned.
- API key string does not appear in any exception message or log output.

Test seam: inject a fake `HttpMessageHandler` (via `HttpClient` constructor) to simulate timeout, error, and success responses. No real network calls.

## Out of Scope

- Retry logic for transient geolocation failures.
- Caching geolocation across restarts (the existing first-run model is unchanged).
- Changing the IpStack API endpoint or authentication mechanism.
- Changing what happens when `IpStackApi` configuration section is entirely absent.

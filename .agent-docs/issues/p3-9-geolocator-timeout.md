# Issues: p3-9-geolocator-timeout

> Work complete — PR ready to merge.

## Add `GeolocationUnavailableException` and Geolocator timeout handling (#382)

**Blocked by**: None

**User stories**: 1, 2, 3

### What to build

New `GeolocationUnavailableException` in `HueShift2.Configuration`. Refactor `Geolocator` to use a constructor-injected `HttpClient` with a 10-second timeout (registered via `IHttpClientFactory`). Wrap the HTTP call and JSON parse in `try/catch`; throw `GeolocationUnavailableException` on any failure. Exception messages must never include the API key. Validate `latitude` and `longitude` before constructing `Geolocation`; throw on parse failure. Add `GeolocatorTests` with a fake `HttpMessageHandler` covering: timeout, malformed response, success, key exclusion.

### Acceptance criteria

- [x] `GeolocationUnavailableException` class exists in `HueShift2.Configuration`
- [x] `Geolocator` uses an injected `HttpClient` (not `new HttpClient()`)
- [x] Injected `HttpClient` has a 10-second timeout
- [x] Any HTTP or network failure throws `GeolocationUnavailableException`
- [x] Any JSON parse failure throws `GeolocationUnavailableException`
- [x] No exception message or log output contains the API key string
- [x] `GeolocatorTests` covers: timeout, malformed response, success, key exclusion

---

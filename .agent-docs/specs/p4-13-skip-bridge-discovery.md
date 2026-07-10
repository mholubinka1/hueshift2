# Skip Bridge Discovery When Stored IP Is Reachable

## Problem Statement

Every HueShift2 startup triggers a full 30-second network discovery scan for a Hue Bridge, even when a working Bridge IP is already stored in the config file. On a congested network this delays startup significantly and generates unnecessary broadcast traffic. `LightingConfigFileManager.Assert()` runs discovery unconditionally — it does not attempt to verify the stored IP before scanning.

## Solution

Before running network discovery, attempt a lightweight HTTP health check against the stored Bridge IP. If it responds, skip discovery entirely and proceed with the stored IP. Only fall back to full discovery if the stored IP is unreachable.

## User Stories

1. As a user with a stable home network, I want the app to start in under 5 seconds so that restarts (after power cuts, updates, etc.) are not noticeably slow.
2. As a user whose Bridge has changed IP (e.g. after a router reset), I want the app to fall back to network discovery and update the config so that I don't need to edit config files manually.
3. As a developer, I want first-run (no config file) to run Bridge discovery exactly once so that the setup flow is not slower than today.

## Implementation Decisions

### Health check

In `LightingConfigFileManager.Assert()`, before calling `DiscoverBridgesOnNetwork()`, attempt:

```
GET http://{bridgeIp}/api
```

with a short timeout (3–5 seconds). If the request returns any HTTP response (including `403 Unauthorized`), the Bridge is reachable — skip discovery and return. If the request times out or throws a network exception, fall through to discovery.

The health check does not require an API key; the Bridge root API endpoint responds to unauthenticated requests.

### `LightingConfigFileManager`

- Add a private `IsBridgeReachable(string ip)` method (or similar) that performs the health check via an injected `HttpClient`.
- `Assert()` calls `IsBridgeReachable(bridgeIp)` first. If reachable, log "Bridge reachable at {ip} — skipping discovery" and return. If not, proceed to `DiscoverBridgesOnNetwork()` as today.
- `Generate()` is unchanged (no stored IP exists on first run; discovery always runs once).

### `IHttpClientFactory` / `HttpClient` injection

`LightingConfigFileManager` currently instantiates `HttpBridgeLocator` and `Geolocator` directly. Inject an `HttpClient` (or `IHttpClientFactory`) for the health check, consistent with the approach taken in P3-9 for `Geolocator`.

### First run

No change to `Generate()`. First run discovers the Bridge exactly once; the result is written to the config file. The next startup calls `Assert()`, which now health-checks the stored IP first.

### `DiscoveryTimeoutSeconds`

The discovery scan timeout is made config-driven by P3-11. This feature uses that value when discovery does fall back — no additional config property needed here.

## Testing Decisions

**`LightingConfigFileManagerTests`** — new test class in `HueShift2.Tests/Configuration/`:

- Stored IP health check succeeds (fake `HttpMessageHandler` returns 200) → `DiscoverBridgesOnNetwork` is not called; method returns immediately.
- Stored IP health check times out or throws → `DiscoverBridgesOnNetwork` is called; if new IP found, config is updated.
- First run (`Generate()` path) → `DiscoverBridgesOnNetwork` called exactly once.

Test seam: inject a fake `HttpMessageHandler` for the health check and a fake `IBridgeLocator` to verify whether discovery was triggered.

Prior art: `LightControllerRefreshTests` DI pattern.

## Out of Scope

- Persistent Bridge connection tracking across polls (health check is startup-only).
- Supporting multiple Bridges.
- Changing how the discovered Bridge IP is written to the config file.
- Changing the discovery scan implementation itself.

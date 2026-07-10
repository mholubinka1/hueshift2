# Remove Runtime Mutation of HueShiftOptions

## Problem Statement

After Bridge registration, `LocalHueClientManager` writes the API key back by mutating `optionsDelegate.CurrentValue.BridgeProperties.ApiKey` directly. This is an invisible side effect on shared configuration state: any code that cached a reference to `BridgeProperties` before registration sees stale data, and the flow of the key from registration through to the config file is implicit and untraceable. The options object should be read-only at runtime.

## Solution

`RegisterApplication()` already returns the API key as a string. Remove the assignment to `optionsDelegate.CurrentValue.BridgeProperties.ApiKey` and use the returned key directly in `client.Initialize()`. The config file — written via `IConfigFileHelper` — remains the single source of truth.

## User Stories

1. As a developer, I want the `HueShiftOptions` object to never be mutated at runtime, so that configuration state is predictable and traceable.
2. As a developer, I want the API key flow (registration → config file → client) to be explicit, so that I can follow it in code without tracking side effects.

## Implementation Decisions

### `LocalHueClientManager.AssertConnected()`

Remove the assignment:
```csharp
optionsDelegate.CurrentValue.BridgeProperties.ApiKey = await RegisterApplication(retryInterval, cts.Token);
```

Replace with a local variable:
```csharp
var apiKey = await RegisterApplication(retryInterval, cts.Token);
```

Pass `apiKey` directly to `client.Initialize(apiKey)`.

`configHelper.AddOrUpdateSetting(...)` in `RegisterApplication()` already persists the key to disk — this does not change.

### `RegisterApplication()`

No signature change. The method already returns the API key; it continues to do so. The `configHelper.AddOrUpdateSetting(...)` call inside `RegisterApplication()` that writes the key to the config file is unchanged.

### Options object

`HueShiftOptions.BridgeProperties.ApiKey` must not be set anywhere at runtime. After this change, any call to `optionsDelegate.CurrentValue.BridgeProperties.ApiKey` post-registration returns the value from the config file (loaded at startup), not the just-registered key — which is correct, since the key is now in the file.

## Testing Decisions

**Unit tests on `AssertConnected()`**: verify that `HueShiftOptions.BridgeProperties.ApiKey` has the same value before and after a registration flow is simulated. Verify that `client.Initialize` is called with the key returned by `RegisterApplication()`, not with the options value. Prior art: `LightControllerRefreshTests` pattern with substituted interfaces.

Test seam: `ILocalHueClient` (for `CheckConnection`, `RegisterAsync`, `Initialize`) and `IConfigFileHelper` (for `AddOrUpdateSetting`). Both are already injected.

## Out of Scope

- Config file encryption or secrets management.
- Changing how the API key is read on startup.
- Changing the registration retry or timeout behaviour (that is P3-11).

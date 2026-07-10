# Replace Magic Numbers with Named Constants and Config-Driven Values

## Problem Statement

Several numeric literals are embedded directly in implementation code with no name or documentation. Adjusting registration timeouts for a slow Bridge, changing the discovery scan window, or understanding what `254` means in a brightness reset requires reading the surrounding code carefully. Some values (registration timeout, discovery timeout) should be adjustable per deployment without a rebuild.

## Solution

Promote deployment-specific values to `HueShiftOptions.BridgeProperties`. Introduce named constants for values that are fixed invariants (`MaxBrightness`, `XYComparisonEpsilon`). Replace all remaining inline literals in the affected files.

## User Stories

1. As a user with a slow Bridge, I want to configure a longer registration timeout in the config file so that I don't need to rebuild the app.
2. As a developer, I want magic numbers replaced with named constants so that the intent of each value is clear at the point of use.
3. As a developer, I want the Bridge discovery timeout to be configurable so that it can be tuned for congested networks.

## Implementation Decisions

### `HueShiftOptions.BridgeProperties`

Add three new properties with defaults matching current hardcoded values:

```csharp
public int RegistrationTimeoutSeconds { get; set; } = 120;
public double RegistrationRetryIntervalSeconds { get; set; } = 10.0;
public int DiscoveryTimeoutSeconds { get; set; } = 30;
```

### `LocalHueClientManager`

Replace:
- `const int cancelAfter = 120` → `appOptionsDelegate.CurrentValue.BridgeProperties.RegistrationTimeoutSeconds`
- `const double retryInterval = 10.0` → `appOptionsDelegate.CurrentValue.BridgeProperties.RegistrationRetryIntervalSeconds`

Pass these as parameters (or read inline) in `AssertConnected()` and `RegisterApplication()`.

### `LightingConfigFileManager`

Replace:
- `TimeSpan.FromSeconds(30)` in `DiscoverBridgesOnNetwork()` → `TimeSpan.FromSeconds(options.BridgeProperties.DiscoveryTimeoutSeconds)` where `options` is read from `IOptionsMonitor<HueShiftOptions>`. `LightingConfigFileManager` currently receives `IConfiguration` but not `IOptionsMonitor`; inject it.

### `LightControlPair`

Add:
```csharp
private const byte MaxBrightness = 254;
```

Replace both occurrences of `254` in `RequiresSync()` with `MaxBrightness`.

### `ExtensionMethods`

Add:
```csharp
private const double XYComparisonEpsilon = 1e-8;
```

Replace `0.00000001d` in `DoubleEquals()` with `XYComparisonEpsilon`.

### `TransitionSettlingPeriodSeconds`

Already a named constant in `LightControlPair` — no change needed.

## Testing Decisions

**Config-driven timeout tests** in a new or extended test class for `LocalHueClientManager`:

- `RegistrationTimeoutSeconds = 30` in config → `CancellationTokenSource.CancelAfter` called with 30 seconds (not 120).
- `RegistrationRetryIntervalSeconds = 5` in config → retry delay is 5 seconds.

These tests inject a fake `ILocalHueClient` and controlled `IOptionsMonitor<HueShiftOptions>`.

**Static analysis / review**: no numeric literals in `LocalHueClientManager`, `LightingConfigFileManager`, `LightControlPair` (for 254), or `ExtensionMethods` (for `0.00000001d`) after the change.

Prior art: `LightControllerRefreshTests` for DI pattern; `LightControlPairManualOverrideTests` for direct construction.

## Out of Scope

- Making `TransitionSettlingPeriodSeconds` config-driven (already a named constant; no deployment scenario requires tuning it).
- Changing default values from their current hardcoded equivalents.
- Moving `XYComparisonEpsilon` to config (it is a fixed algorithmic tolerance, not a deployment parameter).

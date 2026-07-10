# Issues: p3-11-named-constants

## Add `MaxBrightness` and `XYComparisonEpsilon` named constants (#383)

**Blocked by**: None

**User stories**: 2

### What to build

In `LightControlPair`, add `private const byte MaxBrightness = 254;` and replace both occurrences of `254` in `RequiresSync()`. In `ExtensionMethods`, add `private const double XYComparisonEpsilon = 1e-8;` and replace `0.00000001d` in `DoubleEquals()`. Pure rename — no behaviour change.

### Acceptance criteria

- [x] `private const byte MaxBrightness = 254` exists in `LightControlPair`
- [x] No occurrence of the literal `254` remains in `LightControlPair.RequiresSync()`
- [x] `private const double XYComparisonEpsilon = 1e-8` exists in `ExtensionMethods`
- [x] No occurrence of `0.00000001d` remains in `ExtensionMethods.DoubleEquals()`
- [x] All existing tests still pass

---

## Promote registration and discovery timeouts to config-driven values (#384)

**Blocked by**: None

**User stories**: 1, 3

### What to build

Add `RegistrationTimeoutSeconds` (default 120), `RegistrationRetryIntervalSeconds` (default 10.0), and `DiscoveryTimeoutSeconds` (default 30) to `BridgeProperties`. Replace `const int cancelAfter = 120` and `const double retryInterval = 10.0` in `LocalHueClientManager` with the options values. Replace `TimeSpan.FromSeconds(30)` in `LightingConfigFileManager.DiscoverBridgesOnNetwork()` with the config value; inject `IOptionsMonitor<HueShiftOptions>` if not already present. Add tests verifying the config values are respected.

### Acceptance criteria

- [x] `BridgeProperties` has `RegistrationTimeoutSeconds` (default 120), `RegistrationRetryIntervalSeconds` (default 10.0), `DiscoveryTimeoutSeconds` (default 30)
- [x] No inline literals for these values remain in `LocalHueClientManager` or `LightingConfigFileManager`
- [x] Setting `RegistrationTimeoutSeconds = 30` causes cancellation after ~30 seconds
- [ ] Setting `RegistrationRetryIntervalSeconds = 5` causes retry delay of ~5 seconds (not directly testable without Task.Delay abstraction)
- [x] All existing tests pass

---

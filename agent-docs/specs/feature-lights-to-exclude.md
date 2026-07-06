# Lights To Exclude

## Problem Statement

HueShift2 controls all lights it discovers on the Bridge. There is no way for a user to tell the app to leave a specific light alone. Any light the user does not want managed — a bedside lamp, a decorative fixture, a light on a different schedule — is subject to HueShift2's colour temperature and brightness commands with no opt-out.

## Solution

Users can list light IDs and/or Names in a `LightsToExclude` array in the config file. Matched lights enter the `Excluded` Light Control State and receive no commands from HueShift2. The exclusion is evaluated on every poll cycle: adding a light to the list excludes it on the next poll; removing it returns it to `HueShiftControlled` and queues a Sync to bring it back in line.

## User Stories

1. As a user, I want to exclude a light by its ID so that HueShift2 never adjusts it.
2. As a user, I want to exclude a light by its Name so that I don't need to look up Bridge-assigned IDs.
3. As a user, I want to add a light to `LightsToExclude` and have the change take effect on the next poll, without restarting the service.
4. As a user, I want to remove a light from `LightsToExclude` and have it return to HueShift control on the next poll, receiving a Sync to snap it to the current expected state.
5. As a user, I want unrecognised entries in `LightsToExclude` to produce a log warning so that I know about typos or stale IDs, without stopping the service.
6. As a user, I want the warning for an unrecognised entry to appear only once per session so that my logs are not flooded.

## Implementation Decisions

### `HueShiftOptions`

Add `IList<string> LightsToExclude { get; set; }` with a default of an empty list. Accepts both IDs (e.g. `"3"`) and Names (e.g. `"Living Room Lamp"`) in the same flat list.

### `LightControlPair`

Add two `internal` methods, consistent with the existing `TakeManualOverride()` / `ReturnToControl()` private pattern:

- `internal void Exclude()` — sets `AppControlState = Excluded`, clears `SyncRequired`
- `internal void Unexclude()` — sets `AppControlState = HueShiftControlled`, sets `SyncRequired = true`

### `LightRegistry`

Inject `IOptionsMonitor<HueShiftOptions>` (the established pattern across all services). Add `HashSet<string> _warnedExclusions` to track entries that have already triggered a warning this session.

In `Discover()`, after the existing Refresh / new-light initialisation loop, apply exclusion transitions for every registered light:

- If the light's ID or Name is in `LightsToExclude` and its state is not already `Excluded`: call `Exclude()`
- If the light's ID or Name is not in `LightsToExclude` and its state is `Excluded`: call `Unexclude()`
- For newly-discovered lights that are in the exclusion list: skip `UpdateExpectedState` and `MarkForSync`

After processing all discovered lights, check every entry in `LightsToExclude` against the registered lights. For any entry that matched no light by ID or Name and has not previously warned: log a warning and add it to `_warnedExclusions`.

### `ILightRegistry`

No interface changes needed. `Discover()` signature is unchanged; the options are read internally from the monitor.

### State machine invariant

`Reset()` on `LightControlPair` already guards against `Excluded` (returns early). `CanReceiveCommand()` already returns `false` for non-`HueShiftControlled` states. No changes needed to these paths.

### `LightController.Transition()`

The `UpdateExpectedState` call in `Transition()` is made for all lights regardless of state. Excluded lights can receive `UpdateExpectedState` here without harm — it has no side effects on control state — but the loop could be tightened to skip `Excluded` lights. **Decision: leave `Transition()` unchanged for now.** `CanReceiveCommand()` already prevents excluded lights from receiving `ExecuteCommand`. `UpdateExpectedState` on an excluded light is benign and costs nothing.

## Testing Decisions

Two test classes, mirroring the existing conventions in `HueShift2.Tests/Control/`:

**`LightControlPairExclusionTests`**

Unit tests on `LightControlPair` directly. Cover:
- `Exclude()` sets state to `Excluded` and clears `SyncRequired`
- `Unexclude()` sets state to `HueShiftControlled` and sets `SyncRequired`
- `Exclude()` on an already-excluded pair is idempotent
- `Reset()` on an excluded pair is a no-op (state stays `Excluded`)

Prior art: `LightControlPairManualOverrideTests` — direct construction, no mocks.

**`LightRegistryDiscoverTests`**

Tests on `LightRegistry.Discover()` with a substituted `ILocalHueClient` and `IOptionsMonitor<HueShiftOptions>`. Cover:
- Light in exclusion list by ID on first discovery → `Excluded`, no Sync dispatched
- Light in exclusion list by Name on first discovery → `Excluded`, no Sync dispatched
- Light added to exclusion list after discovery → `Excluded` on next `Discover()` call
- Light removed from exclusion list → `HueShiftControlled` + `SyncRequired` on next `Discover()` call
- Unmatched exclusion entry → warning logged once; second `Discover()` does not re-log

Prior art: `LightControllerRefreshTests` — uses real `LightRegistry` with substituted `ILocalHueClient`.

## Out of Scope

- Excluding entire rooms or groups
- Wildcard or regex matching on light names
- Persisting exclusion state across restarts (the state is re-derived from config on each `Discover()`)
- UI or CLI tooling for managing the exclusion list

## Further Notes

`LightControlState.Excluded` already exists in the enum and is already guarded in `LightControlPair.Refresh()` (`case LightControlState.Excluded: break`). CONTEXT.md already documents the `Excluded` state with its live-apply semantics.

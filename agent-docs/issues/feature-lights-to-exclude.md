# Issues: feature-lights-to-exclude

## Add `Exclude()` and `Unexclude()` to `LightControlPair` (#376)

**Blocked by**: None

**User stories**: 1, 2, 3, 4

### What to build

Add `internal void Exclude()` and `internal void Unexclude()` to `LightControlPair`, consistent with the existing `TakeManualOverride()` / `ReturnToControl()` private pattern.

- `Exclude()` sets `AppControlState = Excluded` and clears `SyncRequired`
- `Unexclude()` sets `AppControlState = HueShiftControlled` and sets `SyncRequired = true`

Add `LightControlPairExclusionTests` covering: `Exclude()` sets state and clears sync; `Unexclude()` sets state and queues sync; `Exclude()` is idempotent; `Reset()` on an excluded pair is a no-op.

### Acceptance criteria

- [ ] `Exclude()` sets `AppControlState` to `Excluded`
- [ ] `Exclude()` clears `SyncRequired`
- [ ] `Unexclude()` sets `AppControlState` to `HueShiftControlled`
- [ ] `Unexclude()` sets `SyncRequired` to `true`
- [ ] Calling `Exclude()` on an already-excluded pair is idempotent (state remains `Excluded`, `SyncRequired` remains `false`)
- [ ] `Reset()` on an excluded pair is a no-op — state stays `Excluded`
- [ ] All new tests pass

---

## Add `LightsToExclude` config and wire exclusion into `LightRegistry` (#377)

**Blocked by**: #376

**User stories**: 1, 2, 3, 4, 5, 6

### What to build

Add `IList<string> LightsToExclude { get; set; }` (default: empty list) to `HueShiftOptions`. Inject `IOptionsMonitor<HueShiftOptions>` into `LightRegistry` (consistent with all other services). Add `HashSet<string> _warnedExclusions` field.

In `LightRegistry.Discover()`, after the existing Refresh / new-light initialisation loop, apply exclusion transitions for every registered light:

- If the light's ID or Name matches an entry in `LightsToExclude` and is not already `Excluded`: call `Exclude()`
- If the light's ID or Name does not match any entry and is currently `Excluded`: call `Unexclude()`
- For newly-discovered lights that match the exclusion list: skip `UpdateExpectedState` and `MarkForSync`

After processing all lights, check every entry in `LightsToExclude` against registered lights. For any entry that matched no light by ID or Name and has not previously warned: log a warning and add it to `_warnedExclusions`.

Add `LightRegistryDiscoverTests` covering exclusion by ID, exclusion by Name, live addition (next `Discover()` excludes), live removal (next `Discover()` unexcludes + sets `SyncRequired`), and unmatched entry warns exactly once.

### Acceptance criteria

- [ ] `HueShiftOptions` has `LightsToExclude` property (default empty list)
- [ ] A light whose ID appears in `LightsToExclude` is in `Excluded` state after `Discover()`
- [ ] A light whose Name appears in `LightsToExclude` is in `Excluded` state after `Discover()`
- [ ] An excluded light does not receive `UpdateExpectedState` or `MarkForSync` on first discovery
- [ ] Adding a light to `LightsToExclude` causes it to be `Excluded` on the next `Discover()` call
- [ ] Removing a light from `LightsToExclude` causes it to return to `HueShiftControlled` with `SyncRequired = true` on the next `Discover()` call
- [ ] An unmatched entry in `LightsToExclude` produces exactly one log warning per session
- [ ] All new tests pass

---

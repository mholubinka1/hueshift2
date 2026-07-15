> Work complete — PR ready to merge.

# Issues: feature-immutable-light-state

## Convert Colour to a record and fix equality

**GitHub**: #398

**Blocked by**: None

**User stories**: 3, 4

### What to build

Convert `Colour` from a mutable class to an init-only class with `init`-only properties. Override `Equals(Colour other)` to use `ExtensionMethods.ArrayEquals` for `ColourCoordinates` value comparison. Fix `Equals(object obj)` to cast to `Colour` instead of `AppLightState`. Fix all `NotImplementedException` branches in `Equals(Colour)`. Override `GetHashCode()` consistently with mode-gating. Add `ColourEqualityTests` covering: identical XY coordinates compare equal, different XY unequal, CT-only equality by value, `Equals(object)` with a boxed Colour, null coordinate handling.

### Acceptance criteria

- [x] `Colour` has all five properties (`Mode`, `ColourCoordinates`, `ColourTemperature`, `Hue`, `Saturation`) `init`-only
- [x] `Colour.Equals(Colour other)` uses `ExtensionMethods.ArrayEquals` for `ColourCoordinates` and has no `NotImplementedException` branches
- [x] `Colour.Equals(object obj)` casts to `Colour`, not `AppLightState`
- [x] `Colour.GetHashCode()` is consistent with `Equals` (mode-gated switch)
- [x] New `ColourEqualityTests` pass covering XY value equality, CT equality, object overload, and null coordinates
- [x] All existing tests pass unchanged

---

## Convert AppLightState to a record and remove cloning

**GitHub**: #399

**Blocked by**: None

**User stories**: 1, 2

### What to build

Convert `AppLightState` from a mutable class to an init-only class with `init`-only properties (`Brightness`, `Colour`). Add `WithBrightness`/`WithColour` factory methods. Remove `IDeepCloneable<T>` from `AppLightState`, `Colour`, `LightProperties`, and `Transition`; delete `IDeepCloneable.cs`. Update `CachedControlPair` to replace `DeepClone()` calls with direct assignment.

### Acceptance criteria

- [x] `AppLightState` has `Brightness` and `Colour` `init`-only with `WithBrightness`/`WithColour` factory methods
- [x] `AppLightState` does not implement `IDeepCloneable<AppLightState>` and has no `DeepClone()` method
- [x] `Colour` does not implement `IDeepCloneable<Colour>` and has no `DeepClone()` method
- [x] `LightProperties` and `Transition` no longer implement `IDeepCloneable<T>`; `IDeepCloneable.cs` is deleted
- [x] `CachedControlPair` assigns `ExpectedLight` and `Properties` directly without calling `DeepClone()`
- [x] All existing tests pass unchanged

---

## Rewrite LightControlPair mutation sites

**GitHub**: #400

**Blocked by**: #398, #399

**User stories**: 1, 2

### What to build

Replace all in-place mutations of `ExpectedLight` and its `Colour` in `LightControlPair` with factory method calls (`WithBrightness`/`WithColour`) that produce new instances atomically. Delete `ClearColourState()` — it exists solely to zero Colour fields before `ChangeColour()` sets new ones, a step that is unnecessary once `ChangeColour()` constructs a complete `Colour` in one operation.

### Acceptance criteria

- [x] `LightControlPair.Refresh()` updates Expected Light Brightness via `WithBrightness` on `this.ExpectedLight`
- [x] `LightControlPair.UpdateExpectedState()` updates Expected Light Brightness via `WithBrightness`
- [x] `LightControlPair.ChangeColour()` constructs a new `Colour` in a single expression per colour mode (XY, CT, or default) and assigns it via `WithColour` on `this.ExpectedLight`
- [x] `ClearColourState()` is deleted
- [x] No remaining direct property assignments on `this.ExpectedLight` or `this.ExpectedLight.Colour`
- [x] All existing tests pass unchanged

---

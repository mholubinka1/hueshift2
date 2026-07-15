# Issues: feature-immutable-light-state

## Convert Colour to a record and fix equality

**GitHub**: #398

**Blocked by**: None

**User stories**: 3, 4

### What to build

Convert `Colour` from a mutable class to a C# `record class` with `init`-only properties. Override `Equals(Colour other)` to use `ExtensionMethods.ArrayEquals` for `ColourCoordinates` value comparison. Fix `Equals(object obj)` to cast to `Colour` instead of `AppLightState`. Fix all `NotImplementedException` branches in `Equals(Colour)`. Override `GetHashCode()` consistently. Add `ColourEqualityTests` covering: identical XY coordinates compare equal, different XY unequal, CT-only equality by value, `Equals(object)` with a boxed Colour, null coordinate handling.

### Acceptance criteria

- [ ] `Colour` is a `record class` with all five properties (`Mode`, `ColourCoordinates`, `ColourTemperature`, `Hue`, `Saturation`) `init`-only
- [ ] `Colour.Equals(Colour other)` uses `ExtensionMethods.ArrayEquals` for `ColourCoordinates` and has no `NotImplementedException` branches
- [ ] `Colour.Equals(object obj)` casts to `Colour`, not `AppLightState`
- [ ] `Colour.GetHashCode()` is consistent with `Equals`
- [ ] New `ColourEqualityTests` pass covering XY value equality, CT equality, object overload, and null coordinates
- [ ] All existing tests pass unchanged

---

## Convert AppLightState to a record and remove cloning

**GitHub**: #399

**Blocked by**: None

**User stories**: 1, 2

### What to build

Convert `AppLightState` from a mutable class to a C# `record class` with `init`-only properties (`Brightness`, `Colour`). Remove `IDeepCloneable<AppLightState>` from `AppLightState` and delete its `DeepClone()` method. Remove `IDeepCloneable<Colour>` from `Colour` and delete its `DeepClone()` method. Update `CachedControlPair` to replace `light.ExpectedLight.DeepClone()` with a direct assignment `light.ExpectedLight`. The `IDeepCloneable<T>` interface is retained — `LightProperties` and `Transition` still implement it.

### Acceptance criteria

- [ ] `AppLightState` is a `record class` with `Brightness` and `Colour` `init`-only
- [ ] `AppLightState` does not implement `IDeepCloneable<AppLightState>` and has no `DeepClone()` method
- [ ] `Colour` does not implement `IDeepCloneable<Colour>` and has no `DeepClone()` method
- [ ] `IDeepCloneable<T>` interface file is unchanged (still implemented by `LightProperties` and `Transition`)
- [ ] `CachedControlPair` assigns `ExpectedLight` directly without calling `DeepClone()`
- [ ] All existing tests pass unchanged

---

## Rewrite LightControlPair mutation sites

**GitHub**: #400

**Blocked by**: #398, #399

**User stories**: 1, 2

### What to build

Replace all in-place mutations of `ExpectedLight` and its `Colour` in `LightControlPair` with `with`-expressions that produce new instances atomically. Specifically: the `Refresh()` Brightness update, both `UpdateExpectedState()` Brightness assignments, and `ChangeColour()` which constructs a fresh `Colour` per colour mode in a single expression. Delete `ClearColourState()` — it exists solely to zero Colour fields before `ChangeColour()` sets new ones, a step that is unnecessary once `ChangeColour()` constructs a complete `Colour` in one operation.

### Acceptance criteria

- [ ] `LightControlPair.Refresh()` updates Expected Light Brightness via a `with`-expression on `this.ExpectedLight`
- [ ] `LightControlPair.UpdateExpectedState()` updates Expected Light Brightness via `with`-expressions
- [ ] `LightControlPair.ChangeColour()` constructs a new `Colour` in a single expression per colour mode (XY, CT, or default) and assigns it via a `with`-expression on `this.ExpectedLight`
- [ ] `ClearColourState()` is deleted
- [ ] No remaining direct property assignments on `this.ExpectedLight` or `this.ExpectedLight.Colour`
- [ ] All existing tests pass unchanged

---

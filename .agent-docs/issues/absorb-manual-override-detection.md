# Issues: absorb-manual-override-detection

## Move Manual Override and Drift detection into LightControlPair

**GitHub**: #396

**Blocked by**: None

**User stories**: 1, 2, 3

### What to build

Add `IsManualOverride(int minCt, int maxCt)` and `HasDrifted()` as private methods on `LightControlPair`, reading `this.NetworkLight` and calling `ExtensionMethods.TryXyToCt` for XY→CT conversion. Update the two call sites in `Refresh()` to use the new private methods directly. Delete `IsManualOverride(this State, int, int)`, `HasDrifted(this State, AppLightState)`, and the dead `Equals(this State, AppLightState, int, int)` overload from `ExtensionMethods`, along with their `#region` blocks.

### Acceptance criteria

- [ ] `LightControlPair` has private `IsManualOverride(int minCt, int maxCt)` and `HasDrifted()` methods implementing identical logic to the deleted extension methods
- [ ] `LightControlPair.Refresh()` calls the private methods directly, with no reference to `ExtensionMethods.IsManualOverride` or `ExtensionMethods.HasDrifted`
- [ ] `ExtensionMethods` no longer contains `IsManualOverride`, `HasDrifted`, or `Equals(this State, AppLightState, int, int)`
- [ ] `TryXyToCt` remains in `ExtensionMethods` (still used by `Filter()`)
- [ ] All existing `LightControlPairManualOverrideTests` pass unchanged
- [ ] All other tests pass unchanged

---

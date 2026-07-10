> Work complete — PR ready to merge.

# Issues: p2-8-remove-options-mutation

## Remove runtime mutation of `HueShiftOptions` in `LocalHueClientManager` (#381)

**Blocked by**: None

**User stories**: 1, 2

### What to build

Remove the assignment `optionsDelegate.CurrentValue.BridgeProperties.ApiKey = await RegisterApplication(...)` from `AssertConnected()`. Capture the returned key in a local variable and pass it directly to `client.Initialize(apiKey)`. The `configHelper.AddOrUpdateSetting(...)` call inside `RegisterApplication()` is unchanged. Add unit tests verifying the options object is not mutated and `client.Initialize` receives the correct key.

### Acceptance criteria

- [x] `optionsDelegate.CurrentValue.BridgeProperties.ApiKey` is not assigned anywhere in `LocalHueClientManager`
- [x] `client.Initialize` is called with the key returned by `RegisterApplication()`, not with the options value
- [x] `configHelper.AddOrUpdateSetting(...)` call inside `RegisterApplication()` is unchanged
- [x] A test verifies `HueShiftOptions` is not mutated during the registration flow
- [x] A test verifies `client.Initialize` receives the correct key

---

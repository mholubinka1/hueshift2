> Work complete — PR ready to merge.

# Issues: p4-13-skip-bridge-discovery

## Skip Bridge discovery when stored IP is reachable (#388)

**Blocked by**: #384

**User stories**: 1, 2, 3

### What to build

Add a private `IsBridgeReachable(string ip)` method to `LightingConfigFileManager` that sends `GET http://{ip}/api` with a 3–5 second timeout. Any HTTP response (including 403) means reachable; a network exception or timeout means not. In `Assert()`, call `IsBridgeReachable(bridgeIp)` before `DiscoverBridgesOnNetwork()`. If reachable, log and return without scanning. `Generate()` is unchanged. Inject `HttpClient` via `IHttpClientFactory`. Add `LightingConfigFileManagerTests` with a fake `HttpMessageHandler` and fake `IBridgeLocator` covering: reachable (skip), unreachable (discover), first run (once).

### Acceptance criteria

- [x] `Assert()` performs a health check before running discovery
- [x] If health check succeeds, discovery is skipped and method returns
- [x] If health check fails, discovery runs and config is updated if a new IP is found
- [x] `Generate()` behaviour is unchanged
- [x] `LightingConfigFileManagerTests` covers: reachable (skip), unreachable (discover), first run (once)
- [x] All existing tests pass

---

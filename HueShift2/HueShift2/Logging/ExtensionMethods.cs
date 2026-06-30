using HueShift2.Control;
using HueShift2.Helpers;
using HueShift2.Model;
using Microsoft.Extensions.Logging;
using Q42.HueApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HueShift2.Logging
{
    public static class ExtensionMethods
    {
        #region LightManager

        public static void LogSync<T>(this ILogger<T> logger, IEnumerable<string> lightNames, AppLightState target)
        {
            var names = lightNames.ToArray();
            if (!names.Any()) return;
            logger.LogInformation($"[Sync] {names.Length} light(s) → {target} | {string.Join(", ", names)}");
        }

        public static void LogTransition<T>(this ILogger<T> logger, IEnumerable<LightControlPair> commandLights, AppLightState target, TransitionType transitionType)
        {
            var lights = commandLights.ToArray();
            if (!lights.Any()) return;
            var names = string.Join(", ", lights.Select(l => l.Properties.Name));
            if (transitionType == TransitionType.Adaptive)
            {
                var fromCt = lights.First().ExpectedLight.Colour.ColourTemperature;
                var toCt = target.Colour.ColourTemperature;
                logger.LogInformation($"[Adaptive] CT {fromCt} → {toCt} | {lights.Length} light(s) | {names}");
            }
            else
            {
                logger.LogInformation($"[{transitionType}] {lights.Length} light(s) → {target} | {names}");
            }
        }

        public static void LogLightProperties<T>(this ILogger<T> logger, IEnumerable<LightControlPair> lights)
        {
            foreach (var light in lights)
            {
                logger.LogDebug($"ID: {light.Properties.Id,-4} Name: {light.Properties.Name,-20} ModelID: {light.Properties.ModelId,-10} ProductID: {light.Properties.ProductId,-10}");
            }
        }

        public static void LogDiscovery<T>(this ILogger<T> logger,
            IEnumerable<Light> discoveredLights, IEnumerable<LightControlPair> previousLights)
        {
            var numDiscoveredLights = discoveredLights.Count();
            var numPreviousLights = previousLights.Count();
            if (numDiscoveredLights - numPreviousLights >= 0)
            {
                if (numDiscoveredLights - numPreviousLights == 0)
                {
                    logger.LogDebug($"Discovery: no change to lights on the network.");
                }
                else
                {
                    logger.LogInformation($"Discovery: {numDiscoveredLights - numPreviousLights} discovered new lights on the network.");
                    var newLights = discoveredLights.Where(x => previousLights.Select(y => y.Properties.Id).Contains(x.Id));
                    logger.LogLightProperties(newLights.Select(x => new LightControlPair(x)));
                }
            }
            else
            {
                logger.LogInformation($"Discovery: {numPreviousLights - numDiscoveredLights} lights are no longer connected to the network.");
                var lostLights = previousLights.Where(x => discoveredLights.Select(y => y.Id).Contains(x.Properties.Id));
                logger.LogLightProperties(lostLights);
            }
            return;
        }

        public static void LogRefresh<T>(this ILogger<T> logger, IEnumerable<(CachedControlPair stale, LightControlPair current)> pairs)
        {
            var changed = pairs
                .Where(p => p.stale.PowerState != p.current.PowerState || p.stale.AppControlState != p.current.AppControlState)
                .ToList();
            if (!changed.Any()) return;

            var groups = changed.GroupBy(p =>
            {
                var parts = new List<string>();
                if (p.stale.PowerState != p.current.PowerState)
                    parts.Add($"Power state changed from {p.stale.PowerState} to {p.current.PowerState}");
                if (p.stale.AppControlState != p.current.AppControlState)
                    parts.Add($"Control state changed from {p.stale.AppControlState} to {p.current.AppControlState}");
                return string.Join(" | ", parts);
            });

            foreach (var group in groups)
            {
                var entries = group.ToList();
                if (logger.IsEnabled(LogLevel.Debug))
                {
                    foreach (var p in entries)
                    {
                        logger.LogDebug(p.stale.ToString());
                        logger.LogDebug(p.current.ToString());
                    }
                }
                var names = string.Join(", ", entries.Select(p => p.current.Properties.Name));
                logger.LogInformation($"[Refresh] {entries.Count} light(s): {group.Key} | {names}");
            }
        }

        public static void LogSyncConfirmed<T>(this ILogger<T> logger, IEnumerable<(CachedControlPair stale, LightControlPair current)> pairs)
        {
            var entries = pairs.ToList();
            if (!entries.Any()) return;
            if (logger.IsEnabled(LogLevel.Debug))
            {
                foreach (var p in entries)
                {
                    logger.LogDebug(p.stale.ToString());
                    logger.LogDebug(p.current.ToString());
                }
            }
            var names = string.Join(", ", entries.Select(p => p.current.Properties.Name));
            logger.LogInformation($"[Sync] confirmed | {entries.Count} light(s) | {names}");
        }

        public static void LogSyncFailed<T>(this ILogger<T> logger, IEnumerable<(CachedControlPair stale, LightControlPair current)> pairs)
        {
            var entries = pairs.ToList();
            if (!entries.Any()) return;
            if (logger.IsEnabled(LogLevel.Debug))
            {
                foreach (var p in entries)
                {
                    logger.LogDebug(p.stale.ToString());
                    logger.LogDebug(p.current.ToString());
                }
            }
            var names = string.Join(", ", entries.Select(p => p.current.Properties.Name));
            logger.LogWarning($"[Sync] failed → Manual | {entries.Count} light(s) | {names}");
        }

        public static void LogUpdate<T>(this ILogger<T> logger, IEnumerable<LightControlPair> lights)
        {
            logger.LogDebug("New light states in memory:");
            foreach (var light in lights)
            {
                logger.LogDebug($"ID: {light.Properties.Id} Name: {light.Properties.Name} | {light.ExpectedLight}");
            }
        }

        #endregion

        public static string ToLogString(this State @this)
        {
            var log = $"brightness: {@this.Brightness} mode: {@this.ColorMode.ToColourMode()}";
            log += @this.ColorCoordinates == null ? "" : $" xy: [{string.Join(",", @this.ColorCoordinates)}]";
            log += @this.ColorTemperature == null ? "" : $" ct: {@this.ColorTemperature}";
            log += @this.Hue == null ? "" : $" hue: {@this.Hue}";
            log += @this.Saturation == null ? "" : $" sat: {@this.Saturation}";
            return log;
        }
    }
}

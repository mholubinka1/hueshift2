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

        public static void LogCommand<T>(this ILogger<T> logger, IEnumerable<LightControlPair> commandLights, AppLightState target)
        {
            if (commandLights.Any())
            {
                logger.LogInformation($"Sending command to lights:");
                foreach (var light in commandLights)
                {
                    logger.LogInformation($"ID: {light.Properties.Id} Name: {light.Properties.Name} | from: {light.ExpectedLight} | to: {target}");
                }
            }
        }

        public static void LogLightProperties<T>(this ILogger<T> logger, IEnumerable<LightControlPair> lights)
        {
            foreach (var light in lights)
            {
                logger.LogInformation($"ID: {light.Properties.Id,-4} Name: {light.Properties.Name,-20} ModelID: {light.Properties.ModelId,-10} ProductID: {light.Properties.ProductId,-10}");
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

        public static void LogRefresh<T>(this ILogger<T> logger, CachedControlPair stale, LightControlPair refreshed)
        {
            if (stale.PowerState == refreshed.PowerState &&
                stale.AppControlState == refreshed.AppControlState &&
                stale.ResetOccurred == refreshed.ResetOccurred)
            {
                return;
            }
            var refreshMessage = $"Refreshed light | ID: {refreshed.Properties.Id} Name: {refreshed.Properties.Name}";
            if (stale.PowerState != refreshed.PowerState)
            {
                refreshMessage += $" | Power state changed from {stale.PowerState} to {refreshed.PowerState}";
            }
            if (stale.AppControlState != refreshed.AppControlState)
            {
                refreshMessage += $" | Control state changed from {stale.AppControlState} to {refreshed.AppControlState}";
            }
            if (refreshed.ResetOccurred != refreshed.ResetOccurred)
            {
                refreshMessage += refreshed.ResetOccurred ? $" | Light reset." : $" | Reset scheduled.";
            }
            logger.LogDebug(stale.ToString());
            logger.LogDebug(refreshed.ToString());
            logger.LogInformation(refreshMessage);
        }

        public static void LogUpdate<T>(this ILogger<T> logger, IEnumerable<LightControlPair> lights)
        {
            logger.LogInformation($"New light states in memory:");
            foreach (var light in lights)
            {
                logger.LogInformation($"ID: {light.Properties.Id} Name: {light.Properties.Name} | {light.ExpectedLight}");
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

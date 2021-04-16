using HueShift2.Control;
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
            logger.LogInformation($"Sending command to lights:");
            foreach (var light in commandLights)
            {
                logger.LogInformation($"ID: {light.Properties.Id} Name: {light.Properties.Name} | from: {light.ExpectedLight} | to: {target}");
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

        public static void LogRefresh<T>(this ILogger<T> logger, Tuple<LightPowerState, LightControlState, bool> staleProperties, LightControlPair light)
        {
            if (staleProperties.Item1 == light.PowerState &&
                staleProperties.Item2 == light.AppControlState &&
                staleProperties.Item3 == light.ResetOccurred)
            {
                return;
            }
            var refreshMessage = $"Refreshed light | ID: {light.Properties.Id} Name: {light.Properties.Name}";
            if (staleProperties.Item1 != light.PowerState)
            {
                refreshMessage += $" | Power state changed from {staleProperties.Item1} to {light.PowerState}";
            }
            if (staleProperties.Item2 != light.AppControlState)
            {
                refreshMessage += $" | Control state changed from {staleProperties.Item2} to {light.AppControlState}";
            }
            if (staleProperties.Item3 != light.ResetOccurred)
            {
                refreshMessage += staleProperties.Item3 ? $" | Light reset." : $" | Reset scheduled.";
            }
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
    }
}

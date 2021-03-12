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
        public static void LogDiscovery<T>(this ILogger<T> logger, 
            IList<Light> discoveredLights, IList<AppLight> previousLights)
        {
            if (discoveredLights.Count - previousLights.Count >= 0)
            {
                if (discoveredLights.Count - previousLights.Count == 0)
                {
                    logger.LogDebug($"Discovery: no change to lights on the network.");
                }
                else
                {
                    logger.LogInformation($"Discovery: {discoveredLights.Count - previousLights.Count} discovered new lights on the network.");
                    //TODO: log new lights
                }
            }
            else
            {
                logger.LogInformation($"Discovery: {previousLights.Count - discoveredLights.Count} lights are no longer connected to the network.");
                //TODO: log names and ids of removed lights
            }
            return;
        }



        #endregion
    }
}

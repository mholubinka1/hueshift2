using HueShift2.Configuration;
using HueShift2.Control;
using HueShift2.Interfaces;
using HueShift2.Model;
using Q42.HueApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HueShift2.Helpers
{
    public static class ExtensionMethods
    {
        public static DateTime Clamp(this DateTime dt, DateTime min, DateTime max)
        {
            if (dt <= min) return min;
            if (dt >= max) return max;
            return dt;
        }

        public static IDictionary<string, LightControlPair> Trim(this IDictionary<string, LightControlPair> dict, IList<Light> networkLights)
        {
            var trimmedDict = new Dictionary<string, LightControlPair>();
            foreach (var id in networkLights.Select(x => x.Id))
            {
                if (dict.ContainsKey(id))
                {
                    trimmedDict.Add(id, dict[id]);
                }
            }
            return trimmedDict;
        }

        public static void Reset(this IDictionary<string, LightControlPair> controlPairs)
        {
            foreach (var controlPair in controlPairs)
            {
                controlPair.Value.Reset();
            }
        }

        public static LightControlPair[] SelectLightsToControl(this IDictionary<string, LightControlPair> controlPairs)
        {
            var commandLights = controlPairs.Where(x => x.Value.AppControlState == LightControlState.HueShift && x.Value.PowerState == LightPowerState.On)
                .Select(x => x.Value).ToArray();
            return commandLights;
        }

        public static LightControlPair[] Filter(this LightControlPair[] commandLights, AppLightState targetState)
        {
            var targetCt = targetState.Colour.ColourTemperature;
            return commandLights.Where(l =>
            {
                var expectedCt = l.ExpectedLight.Colour.ColourTemperature;
                return expectedCt == null || targetCt == null || Math.Abs(expectedCt.Value - targetCt.Value) > 10;
            }).ToArray();
        }

        public static LightPowerState DeterminePowerState(this State networkLight)
        {
            if (networkLight.IsReachable is null)
            {
                return LightPowerState.Off;
            }
            if ((bool)networkLight.IsReachable)
            {
                return networkLight.On ? LightPowerState.On : LightPowerState.Off;
            }
            return LightPowerState.Off;
        }

        public static ColourMode ToColourMode(this string mode)
        {
            return mode switch
            {
                "xy" => ColourMode.XY,
                "ct" => ColourMode.CT,
                "hs" => ColourMode.HS,
                _ => throw new NotSupportedException(),
            };
        }

        public static LightCommand ToCommand(this AppLightState expectedLight)
        {
            return expectedLight.Colour.Mode switch
            {
                ColourMode.XY => new LightCommand
                {
                    Brightness = expectedLight.Brightness,
                    ColorCoordinates = expectedLight.Colour.ColourCoordinates,
                },
                ColourMode.CT => new LightCommand
                {
                    Brightness = expectedLight.Brightness,
                    ColorTemperature = expectedLight.Colour.ColourTemperature,
                },
                _ => throw new NotImplementedException(),
            };
        }

        #region Light Equality

        public static bool TryXyToCt(double[] xy, out int ct)
        {
            ct = 0;
            if (xy == null || xy.Length < 2) return false;
            var denominator = 0.1858 - xy[1];
            if (Math.Abs(denominator) < 1e-10) return false;
            var n = (xy[0] - 0.3320) / denominator;
            var kelvin = 449 * Math.Pow(n, 3) + 3525 * Math.Pow(n, 2) + 6823.3 * n + 5520.33;
            if (kelvin <= 0 || double.IsNaN(kelvin) || double.IsInfinity(kelvin)) return false;
            ct = (int)(1_000_000 / kelvin);
            return true;
        }

        public static bool Equals(this State @this, AppLightState expectedLight, int minCt, int maxCt)
        {
            if (expectedLight.Colour.ColourTemperature != null)
            {
                var expectedCt = expectedLight.Colour.ColourTemperature.Value;
                if (@this.ColorTemperature != null)
                    return Math.Abs(@this.ColorTemperature.Value - expectedCt) <= 50;
                if (@this.ColorCoordinates != null)
                {
                    if (!TryXyToCt(@this.ColorCoordinates, out var convertedCt)) return false;
                    if (convertedCt < minCt - 50 || convertedCt > maxCt + 50) return false;
                    return Math.Abs(convertedCt - expectedCt) <= 50;
                }
                return false;
            }
            if (expectedLight.Colour.ColourCoordinates != null && @this.ColorCoordinates != null)
                return ArrayEquals(expectedLight.Colour.ColourCoordinates, @this.ColorCoordinates);
            return false;
        }

        public static bool ArrayEquals(double[] @this, double[] other)
        {
            if (@this == null) return other == null;
            if (other == null) return false;
            if (@this.Length != other.Length) return false;
            for (var i = 0; i < @this.Length; i++)
            {
                if (!DoubleEquals(@this[i], other[i])) return false;
            }
            return true;
        }

        public static bool DoubleEquals(double @this, double other)
        {
            var precision = 0.00000001d;
            var equals = (Math.Abs(@this - other) <= precision);
            return equals;
        }

        #endregion

        #region Cloning

        public static double[] DeepClone(this double[] @this)
        {
            if (@this == null) return null;
            var cloned = new List<double>();
            foreach (var element in @this)
            {
                cloned.Add(element);
            }
            return cloned.ToArray();
        }

        #endregion
    }
}

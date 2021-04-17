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
            var filtered = new List<LightControlPair>();
            foreach (var light in commandLights)
            {
                if (!light.ExpectedLight.Equals(targetState))
                {
                    filtered.Add(light);
                }
            }
            return filtered.ToArray();
        }

        public static ColourMode ToColourMode(this string mode)
        {
            switch (mode)
            {
                case "xy":
                    return ColourMode.XY;
                case "ct":
                    return ColourMode.CT;
                default:
                    throw new NotSupportedException();
            }
        }

        public static LightCommand ToCommand(this AppLightState expectedLight)
        {
            switch (expectedLight.Colour.Mode)
            {
                case ColourMode.XY:
                    return new LightCommand
                    {
                        Brightness = expectedLight.Brightness,
                        ColorCoordinates = expectedLight.Colour.ColourCoordinates,
                    };
                case ColourMode.CT:
                    return new LightCommand
                    {
                        Brightness = expectedLight.Brightness,
                        ColorTemperature = expectedLight.Colour.ColourTemperature,
                    };
                case ColourMode.Other:
                case ColourMode.None:
                default:
                    throw new NotImplementedException();
            }
        }

        #region Light Equality

        public static bool ColourEquals(this State @this, AppLightState expectedLight)
        {
            if (expectedLight.Colour.Mode != @this.ColorMode.ToColourMode()) return false;
            switch (expectedLight.Colour.Mode)
            {
                case ColourMode.XY:
                    return ExtensionMethods.ArrayEquals(expectedLight.Colour.ColourCoordinates, @this.ColorCoordinates);
                case ColourMode.CT:
                    return expectedLight.Colour.ColourTemperature == @this.ColorTemperature;
                default:
                    //return this.Hue == lightState.Hue && this.Saturation == lightState.Saturation;
                    throw new NotImplementedException();
            }
        }

        public static bool ArrayEquals(double[] @this, double[] other)
        {
            if (@this == null)
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
    }
}

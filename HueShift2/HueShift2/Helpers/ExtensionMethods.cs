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

        public static string[] SelectLightsToControl(this IDictionary<string, AppLight> dict, bool resumeControl)
        {
            string[] ids;
            if (resumeControl)
            {
                ids = dict.Values
                    .Where(x => x.ControlState == LightControlState.HueShift && x.State.PowerState == LightPowerState.On)
                    .Select(x => x.Id)
                    .ToArray();
            }
            else
            {
                ids = dict.Values
                    .Where(x => x.ControlState != LightControlState.Excluded && x.State.PowerState == LightPowerState.On)
                    .Select(x => x.Id)
                    .ToArray();
            }
            return ids;
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

        #region Light Equality

        public static bool LightEquals(this Light @this, AppLight expectedLight)
        {
            return false;
        }

        #endregion

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
            var equals = (Math.Abs(@this - other) <= 0.00000001);
            return equals;
        }
    }
}

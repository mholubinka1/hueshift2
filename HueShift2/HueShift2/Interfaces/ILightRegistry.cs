using HueShift2.Configuration.Model;
using HueShift2.Control;
using Q42.HueApi;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HueShift2.Interfaces
{
    public interface ILightRegistry
    {
        Task Discover(LightCommand cachedCommand, DateTime currentTime, ColourTemperature ct, TimeSpan syncGracePeriod);
        IReadOnlyDictionary<string, LightControlPair> GetAll();
        void Reset();
    }
}

using HueShift2.Control;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HueShift2.Interfaces
{
    public interface ILightSynchroniser
    {
        Task Synchronise(IReadOnlyDictionary<string, LightControlPair> lights, DateTime currentTime);
    }
}

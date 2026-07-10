using HueShift2.Model;
using System;

namespace HueShift2.Interfaces
{
    public interface ISolarEventProvider
    {
        AdaptiveSolarEvents GetEventsForDate(DateOnly date);
    }
}

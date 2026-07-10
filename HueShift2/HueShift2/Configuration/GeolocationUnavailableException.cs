using System;

namespace HueShift2.Configuration
{
    public class GeolocationUnavailableException : Exception
    {
        public GeolocationUnavailableException(string message, Exception inner = null)
            : base(message, inner) { }
    }
}

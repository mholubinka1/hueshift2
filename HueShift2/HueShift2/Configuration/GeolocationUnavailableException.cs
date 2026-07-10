using System;

namespace HueShift2.Configuration
{
    public class GeolocationUnavailableException : Exception
    {
        public GeolocationUnavailableException() : base() { }
        public GeolocationUnavailableException(string message) : base(message) { }
        public GeolocationUnavailableException(string message, Exception inner) : base(message, inner) { }
    }
}

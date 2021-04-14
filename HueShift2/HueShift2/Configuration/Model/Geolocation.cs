using GeoTimeZone;

namespace HueShift2.Configuration.Model
{
    public class Geolocation
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string TimeZone { get; set; }

        public Geolocation()
        {

        }

        public Geolocation(double latitude, double longitude)
        {
            Latitude = latitude;
            Longitude = longitude;
            TimeZone = TimeZoneLookup.GetTimeZone(latitude, longitude).Result;
        }
    }
}

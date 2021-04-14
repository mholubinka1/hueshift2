using HueShift2.Configuration.Model;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace HueShift2.Configuration
{
    public class Geolocator : IGeoLocator
    {
        private readonly IConfigurationSection config;

        public Geolocator(IConfigurationSection config)
        {
            this.config = config;
        }

        public async Task<Geolocation> Get()
        {
            var geolocationUri = new Uri(config["Uri"] + config["Key"]);
            string geolocationResponse;
            using (var client = new HttpClient())
            {
                geolocationResponse = await client.GetStringAsync(geolocationUri);
            }
            dynamic response = JObject.Parse(geolocationResponse);
            return new Geolocation(
                (double) response.latitude,
                (double) response.longitude);
        }
    }
}

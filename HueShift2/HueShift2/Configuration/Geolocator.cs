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
        private readonly HttpClient httpClient;
        private readonly IConfigurationSection config;

        public Geolocator(HttpClient httpClient, IConfigurationSection config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            this.httpClient = httpClient;
            this.config = config;
        }

        public async Task<Geolocation> Get()
        {
            var baseUri = config["Uri"];
            var fullUri = baseUri + config["Key"];

            string responseBody;
            try
            {
                responseBody = await httpClient.GetStringAsync(fullUri);
            }
            catch (Exception e)
            {
                throw new GeolocationUnavailableException(
                    $"Geolocation request to {baseUri} failed. Check the IpStackApi configuration section.",
                    e);
            }

            try
            {
                dynamic response = JObject.Parse(responseBody);
                double latitude = (double)response.latitude;
                double longitude = (double)response.longitude;
                return new Geolocation(latitude, longitude);
            }
            catch (Exception e)
            {
                throw new GeolocationUnavailableException(
                    $"Geolocation response from {baseUri} could not be parsed. Check the IpStackApi configuration section.",
                    e);
            }
        }
    }
}

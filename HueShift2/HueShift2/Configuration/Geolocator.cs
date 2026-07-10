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
            if (httpClient == null) throw new ArgumentNullException(nameof(httpClient));
            if (config == null) throw new ArgumentNullException(nameof(config));
            this.httpClient = httpClient;
            this.config = config;
        }

        public async Task<Geolocation> Get()
        {
            var baseUri = config["Uri"];
            var fullUri = baseUri + config["Key"];
            var safeUri = GetSafeUri(baseUri);

            string responseBody;
            try
            {
                responseBody = await httpClient.GetStringAsync(fullUri);
            }
            catch (Exception e)
            {
                throw new GeolocationUnavailableException(
                    $"Geolocation request to {safeUri} failed. Check the IpStackApi configuration section.",
                    e);
            }

            try
            {
                var obj = JObject.Parse(responseBody);
                var lat = obj["latitude"];
                var lon = obj["longitude"];
                if (lat == null || lon == null)
                    throw new GeolocationUnavailableException(
                        $"Geolocation response from {safeUri} did not contain latitude and longitude fields. Check the IpStackApi configuration section.");
                return new Geolocation(lat.Value<double>(), lon.Value<double>());
            }
            catch (GeolocationUnavailableException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new GeolocationUnavailableException(
                    $"Geolocation response from {safeUri} could not be parsed. Check the IpStackApi configuration section.",
                    e);
            }
        }

        private static string GetSafeUri(string uri)
        {
            Uri parsed;
            if (uri != null && Uri.TryCreate(uri, UriKind.Absolute, out parsed))
                return parsed.GetLeftPart(UriPartial.Path);
            return "(check IpStackApi:Uri configuration)";
        }
    }
}

using HueShift2.Configuration;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace HueShift2.Tests.Configuration
{
    public class GeolocatorTests
    {
        private const string TestUri = "https://api.ipstack.com/check?fields=latitude,longitude&access_key=";
        private const string TestKey = "test-api-key";

        private static IConfigurationSection BuildConfig(string uri = TestUri, string key = TestKey)
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["IpStack:Uri"] = uri,
                    ["IpStack:Key"] = key,
                })
                .Build();
            return config.GetSection("IpStack");
        }

        private static Geolocator BuildGeolocator(HttpMessageHandler handler, TimeSpan? timeout = null)
        {
            var httpClient = new HttpClient(handler)
            {
                Timeout = timeout ?? TimeSpan.FromSeconds(10),
            };
            return new Geolocator(httpClient, BuildConfig());
        }

        private class TimeoutHandler : HttpMessageHandler
        {
            protected override async Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request, CancellationToken cancellationToken)
            {
                await Task.Delay(Timeout.Infinite, cancellationToken);
                return null!;
            }
        }

        private class FixedResponseHandler : HttpMessageHandler
        {
            private readonly string _body;
            private readonly HttpStatusCode _status;

            public FixedResponseHandler(string body, HttpStatusCode status = HttpStatusCode.OK)
            {
                _body = body;
                _status = status;
            }

            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request, CancellationToken cancellationToken) =>
                Task.FromResult(new HttpResponseMessage(_status)
                {
                    Content = new StringContent(_body),
                });
        }

        [Fact]
        public async Task Get_ThrowsGeolocationUnavailableException_OnTimeout()
        {
            // Given: HttpClient that always times out
            var geolocator = BuildGeolocator(new TimeoutHandler(), timeout: TimeSpan.FromMilliseconds(50));

            // When / Then
            await Assert.ThrowsAsync<GeolocationUnavailableException>(() => geolocator.Get());
        }

        [Fact]
        public async Task Get_ThrowsGeolocationUnavailableException_OnMalformedResponse()
        {
            // Given: response body is not valid JSON
            var geolocator = BuildGeolocator(new FixedResponseHandler("not-json"));

            // When / Then
            await Assert.ThrowsAsync<GeolocationUnavailableException>(() => geolocator.Get());
        }

        [Fact]
        public async Task Get_ReturnsGeolocation_OnValidResponse()
        {
            // Given: well-formed JSON with latitude and longitude
            var json = "{\"latitude\": 51.5, \"longitude\": -0.1}";
            var geolocator = BuildGeolocator(new FixedResponseHandler(json));

            // When
            var result = await geolocator.Get();

            // Then
            Assert.Equal(51.5, result.Latitude, precision: 1);
            Assert.Equal(-0.1, result.Longitude, precision: 1);
        }

        [Fact]
        public async Task Get_ExceptionMessage_DoesNotContainApiKey()
        {
            // Given: timeout will throw an exception
            var geolocator = BuildGeolocator(new TimeoutHandler(), timeout: TimeSpan.FromMilliseconds(50));

            // When
            var ex = await Assert.ThrowsAsync<GeolocationUnavailableException>(() => geolocator.Get());

            // Then: the API key is not in the exception message or inner exception messages
            Assert.DoesNotContain(TestKey, ex.Message);
            if (ex.InnerException != null)
                Assert.DoesNotContain(TestKey, ex.InnerException.Message);
        }
    }
}

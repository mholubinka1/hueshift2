using HueShift2.Configuration;
using HueShift2.Configuration.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Q42.HueApi.Interfaces;
using Q42.HueApi.Models.Bridge;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace HueShift2.Tests.Configuration
{
    public class LightingConfigFileManagerTests
    {
        private class FixedResponseHandler : HttpMessageHandler
        {
            private readonly HttpStatusCode _status;
            public FixedResponseHandler(HttpStatusCode status = HttpStatusCode.OK) => _status = status;
            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request, CancellationToken cancellationToken) =>
                Task.FromResult(new HttpResponseMessage(_status));
        }

        private class ThrowingHandler : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request, CancellationToken cancellationToken) =>
                Task.FromException<HttpResponseMessage>(new HttpRequestException("connection refused"));
        }

        private static IBridgeLocator BridgeLocatorReturning(string ip)
        {
            var locator = Substitute.For<IBridgeLocator>();
            locator.LocateBridgesAsync(Arg.Any<TimeSpan>())
                .Returns(Task.FromResult<IEnumerable<LocatedBridge>>(
                    new[] { new LocatedBridge { BridgeId = "bridge0", IpAddress = ip } }));
            return locator;
        }

        private static LightingConfigFileManager BuildManager(
            HttpMessageHandler healthCheckHandler,
            IBridgeLocator? bridgeLocator = null,
            IGeoLocator? geoLocator = null,
            IConfigFileHelper? configFileHelper = null)
        {
            var healthCheckClient = new HttpClient(healthCheckHandler);
            var options = Options.Create(new HueShiftOptions { BridgeProperties = new BridgeProperties() });
            return new LightingConfigFileManager(
                NullLogger<LightingConfigFileManager>.Instance,
                configFileHelper ?? Substitute.For<IConfigFileHelper>(),
                new ConfigurationBuilder().Build(),
                options,
                healthCheckClient,
                bridgeLocator ?? Substitute.For<IBridgeLocator>(),
                geoLocator ?? Substitute.For<IGeoLocator>());
        }

        [Fact]
        public async Task Assert_SkipsDiscovery_WhenBridgeReachable()
        {
            // Given: stored IP, health check returns 200 OK
            var bridgeLocator = Substitute.For<IBridgeLocator>();
            var manager = BuildManager(new FixedResponseHandler(HttpStatusCode.OK), bridgeLocator: bridgeLocator);

            // When
            await manager.Assert("config.json", "192.168.1.100");

            // Then: discovery not triggered
            await bridgeLocator.DidNotReceive().LocateBridgesAsync(Arg.Any<TimeSpan>());
        }

        [Fact]
        public async Task Assert_SkipsDiscovery_WhenBridgeReturns403()
        {
            // Given: health check returns 403 — bridge is present but rejects unauthenticated request
            var bridgeLocator = Substitute.For<IBridgeLocator>();
            var manager = BuildManager(new FixedResponseHandler(HttpStatusCode.Forbidden), bridgeLocator: bridgeLocator);

            // When
            await manager.Assert("config.json", "192.168.1.100");

            // Then: any HTTP response means reachable — discovery still skipped
            await bridgeLocator.DidNotReceive().LocateBridgesAsync(Arg.Any<TimeSpan>());
        }

        [Fact]
        public async Task Assert_RunsDiscoveryAndUpdatesConfig_WhenBridgeUnreachableAndIpChanged()
        {
            // Given: health check fails; discovery finds a new IP
            const string storedIp = "192.168.1.100";
            const string newIp = "192.168.1.200";
            var bridgeLocator = BridgeLocatorReturning(newIp);
            var configFileHelper = Substitute.For<IConfigFileHelper>();
            var manager = BuildManager(new ThrowingHandler(), bridgeLocator: bridgeLocator, configFileHelper: configFileHelper);

            // When
            await manager.Assert("config.json", storedIp);

            // Then: discovery was triggered and config was updated with the new IP
            await bridgeLocator.Received(1).LocateBridgesAsync(Arg.Any<TimeSpan>());
            configFileHelper.Received(1).AddOrUpdateSetting("config.json", "HueShiftOptions:BridgeProperties:IpAddress", newIp);
        }

        [Fact]
        public async Task Assert_RunsDiscoveryWithoutUpdatingConfig_WhenBridgeUnreachableAndIpUnchanged()
        {
            // Given: health check fails; discovery returns the same IP that was stored
            const string storedIp = "192.168.1.100";
            var bridgeLocator = BridgeLocatorReturning(storedIp);
            var configFileHelper = Substitute.For<IConfigFileHelper>();
            var manager = BuildManager(new ThrowingHandler(), bridgeLocator: bridgeLocator, configFileHelper: configFileHelper);

            // When
            await manager.Assert("config.json", storedIp);

            // Then: discovery was triggered but config was not written (IP unchanged)
            await bridgeLocator.Received(1).LocateBridgesAsync(Arg.Any<TimeSpan>());
            configFileHelper.DidNotReceive().AddOrUpdateSetting(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
        }

        [Fact]
        public async Task Generate_RunsDiscoveryExactlyOnce()
        {
            // Given: first run — no stored IP; geolocator and bridge locator return valid data
            var geoLocator = Substitute.For<IGeoLocator>();
            geoLocator.Get().Returns(Task.FromResult(new Geolocation(51.5, -0.1)));
            var bridgeLocator = BridgeLocatorReturning("192.168.1.100");
            var manager = BuildManager(new ThrowingHandler(), bridgeLocator: bridgeLocator, geoLocator: geoLocator);

            // When
            await manager.Generate("config.json");

            // Then: bridge discovery ran exactly once (no health check path for Generate)
            await bridgeLocator.Received(1).LocateBridgesAsync(Arg.Any<TimeSpan>());
        }
    }
}

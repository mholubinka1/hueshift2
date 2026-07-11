using HueShift2.Configuration.Model;
using HueShift2.Control;
using HueShift2.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Q42.HueApi.Interfaces;
using System.Threading.Tasks;
using Xunit;

namespace HueShift2.Tests.Control
{
    public class LocalHueClientManagerTests
    {
        private static (LocalHueClientManager manager, ILocalHueClient hueClient, HueShiftOptions options) BuildManager(
            string initialApiKey = "",
            int registrationTimeoutSeconds = 120,
            double registrationRetryIntervalSeconds = 10.0)
        {
            var options = new HueShiftOptions
            {
                BridgeProperties = new BridgeProperties
                {
                    ApiKey = initialApiKey,
                    RegistrationTimeoutSeconds = registrationTimeoutSeconds,
                    RegistrationRetryIntervalSeconds = registrationRetryIntervalSeconds,
                },
            };
            var optionsMonitor = Substitute.For<IOptionsMonitor<HueShiftOptions>>();
            optionsMonitor.CurrentValue.Returns(options);

            var hueClient = Substitute.For<ILocalHueClient>();
            var configHelper = Substitute.For<IConfigFileHelper>();
            var config = Substitute.For<IConfiguration>();

            var manager = new LocalHueClientManager(
                NullLogger<LocalHueClientManager>.Instance,
                config,
                optionsMonitor,
                hueClient,
                configHelper);

            return (manager, hueClient, options);
        }

        [Fact]
        public async Task AssertConnected_InitializesClient_WithKeyReturnedByRegistration()
        {
            // Given: no connection, no stored key; registration succeeds with "test-key"
            var (manager, hueClient, _) = BuildManager(initialApiKey: "");
            hueClient.CheckConnection().Returns(Task.FromResult(false));
            hueClient.RegisterAsync("hueshift-2", "Bridge0").Returns(Task.FromResult<string?>("test-key"));

            // When
            await manager.AssertConnected();

            // Then: client was initialised with the key returned by RegisterAsync, not with the options value
            hueClient.Received(1).Initialize("test-key");
        }

        [Fact]
        public async Task AssertConnected_DoesNotMutateOptions_DuringRegistration()
        {
            // Given: no connection, no stored key; registration succeeds with "test-key"
            var (manager, hueClient, options) = BuildManager(initialApiKey: "");
            hueClient.CheckConnection().Returns(Task.FromResult(false));
            hueClient.RegisterAsync("hueshift-2", "Bridge0").Returns(Task.FromResult<string?>("test-key"));
            var before = options.BridgeProperties.ApiKey;

            // When
            await manager.AssertConnected();

            // Then: the options object was not mutated
            Assert.Equal(before, options.BridgeProperties.ApiKey);
        }

        [Fact]
        public async Task AssertConnected_DoesNotInitializeClient_WhenAlreadyConnected()
        {
            // Given: connection already established
            var (manager, hueClient, _) = BuildManager(initialApiKey: "existing-key");
            hueClient.CheckConnection().Returns(Task.FromResult(true));

            // When
            await manager.AssertConnected();

            // Then: client.Initialize is not called
            hueClient.DidNotReceive().Initialize(Arg.Any<string>());
        }

        [Fact]
        public async Task AssertConnected_CancelsRegistration_AfterConfiguredTimeout()
        {
            // Given: no connection, no stored key, registration always fails.
            // RegistrationTimeoutSeconds = 0: CancelAfter(0) calls Cancel() synchronously (per .NET semantics),
            // so the CancellationToken is already cancelled when RegisterApplication checks it.
            // If the code ignored config and used the hardcoded default of 120, CancelAfter(120000) would
            // not fire quickly and the test would hang — proving the config value is read.
            var (manager, hueClient, _) = BuildManager(
                initialApiKey: "",
                registrationTimeoutSeconds: 0,
                registrationRetryIntervalSeconds: 0.0);
            hueClient.CheckConnection().Returns(Task.FromResult(false));
            hueClient.RegisterAsync(Arg.Any<string>(), Arg.Any<string>())
                .Returns(Task.FromException<string?>(new Exception("bridge not ready")));

            // When / Then
            await Assert.ThrowsAsync<OperationCanceledException>(() => manager.AssertConnected());
        }
    }
}

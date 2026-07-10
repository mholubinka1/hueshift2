using System;
using System.Collections.Generic;
using System.Text;

namespace HueShift2.Configuration.Model
{
    public class BridgeProperties
    {
        public string IpAddress { get; set; }
        public string ApiKey { get; set; }
        public int RegistrationTimeoutSeconds { get; set; } = 120;
        public double RegistrationRetryIntervalSeconds { get; set; } = 10.0;
        public int DiscoveryTimeoutSeconds { get; set; } = 30;

        public BridgeProperties()
        {

        }
    }
}

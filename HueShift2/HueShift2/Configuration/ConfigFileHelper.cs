using HueShift2.Configuration.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;

namespace HueShift2.Configuration
{
using System.IO;
using System.Collections.Generic;
    using Newtonsoft.Json.Converters;

    public class ConfigFileHelper : IConfigFileHelper
    {
        private readonly ILogger<ConfigFileHelper> logger;

        public ConfigFileHelper(ILogger<ConfigFileHelper> logger)
        {
            this.logger = logger;;
        }

        private void SetValue<T>(dynamic json, string key, T value)
        {
            var remainingSections = key.Split(":", 2);
            var currentSection = remainingSections[0];
            if (remainingSections.Length > 1)
            {
                var nextSection = remainingSections[1];
                SetValue(json[currentSection], nextSection, value);
            }
            else
            {
                json[currentSection] = value;
            }
        }

        public void AddOrUpdateSetting<T>(string configFilePath, string key, T value)
        {
            try
            {
                string configText = File.ReadAllText(configFilePath);
                dynamic json = JsonConvert.DeserializeObject(configText);
                SetValue(json, key, value);
                var newConfigText = JsonConvert.SerializeObject(json, Formatting.Indented, new StringEnumConverter());
                File.WriteAllText(configFilePath, newConfigText);
                logger.LogInformation($"{key} successfully written to {configFilePath}");
            }
            catch (Exception e)
            {
                logger.LogError(e, $"Failed to write configuration setting: {key}");
            }
            return;
        }
    }
}
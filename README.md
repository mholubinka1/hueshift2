# HueShift-2

Hueshift-2 is inspired by [Andy Kutruff's Hueshift](https://github.com/akutruff/HueShift) and incorporates ideas from [Bas Nijholt's Adaptive Lighting](https://github.com/basnijholt/adaptive-lighting). HueShift-2 adjusts the colour temperature of your Hue compatible lights. Colour temperature is determined by the position of the Sun above the horizon, changing subtly and continuously throughout the day.

These colours help to properly regulate your sleep cycle by maintaining your natural circadian rhythms, improving your mood and overall well-being. The coolest colours occurs around noon when the Sun is highest in the sky, gradually transitioning to warmest colours at sunrise and sunset.

The program continously geolocates your IP address, determining sunrise, sunset and solar noon for your location. The colour temperatures are then scaled according to the position of the Sun above the horizon using circular geometery. When the Sun is below the horizon the lights are set to the warmest possible colour before being dimmed at bedtime.


HueShift-2 is designed to automatically detect when a light is being controlled by you. By default, however, it will regain control of all of your lights at sunrise and sunset.

## Configuration

When the program starts for the first time a configuration file is automatically generated using the default settings from the following template:

```
{
  "HueShiftOptions": {
    "Mode": "Adaptive",
    "PollingFrequency": 2,
    "TransitionInterval": 600,
    "BasicTransitionDuration": 2,
    "AdaptiveTransitionDuration": 30,
    "SolarTransitionDuration": 120,
    "SolarTransitionTimeLimits": {
      "SunriseLower": "06:00:00",
      "SunriseUpper": "08:00:00",
      "SunsetLower": "18:00:00",
      "SunsetUpper": "20:00:00"
    },
    "Sleep": "23:00:00",
    "LightsToExclude": [],
    "BridgeProperties": {
      "IpAddress": "<BRIDGE-IPADDRESS>",
      "ApiKey": "<BRIDGE-APIKEY>"
    },
    "Geolocation": {
      "Latitude": <LATITUDE>,
      "Longitude": <LONGITUDE>,
      "TimeZone": "Europe/London"
    },
    "ColourTemperature": {
      "Coolest": 250,
      "Warmest": 454
    },
    "NightBrightnessPercentage": 60
  }
}
```

This configuration once generated can be modified and reloaded dynamically. The file can be located anywhere on your device, just mount the folder of your choice to the appropriate folder in the `docker-compose.yml`:

```
volumes:
    - <LOCAL-CONFIG-LOCATION>:/config
    - <LOCAL-LOG-LOCATION>:/log
```

## Docker

The latest image can be found on [DockerHub](https://hub.docker.com/repository/docker/mholubinka1/hueshift2/general) and a sample `docker-compose.yml` is included for deployment.

To deploy, navigate to the folder contained the `docker-compose.yml` file:

```
docker compose up -d
```


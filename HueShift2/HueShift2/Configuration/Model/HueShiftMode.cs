using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json.Serialization;

namespace HueShift2.Configuration
{
    [JsonConverter(typeof(StringEnumConverter))] 
    public enum HueShiftMode
    {
        Auto = 0,
        Custom = 1
    }
}

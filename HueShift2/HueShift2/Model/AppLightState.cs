using HueShift2.Model;
using Q42.HueApi;
using System;
using System.Collections.Generic;
using System.Text;

namespace HueShift2.Model
{
    public class AppLightState
    {
        public byte? Brightness { get; set; }
        public string Scene { get; set; }
        public Colour Colour { get; set; }

        public AppLightState(State state)
        {
            this.Brightness = (byte)254;
            this.Colour = new Colour(state);
        }

        public AppLightState(AppLightState appLight)
        {
            this.Brightness = appLight.Brightness;
            this.Scene = appLight.Scene;
            this.Colour = new Colour(appLight.Colour);
        }

        public AppLightState(Colour colour)
        {
            this.Colour = new Colour(colour);
        }

        public override string ToString()
        {
            var @base = this.Brightness == null ? "" : $"brightness: {this.Brightness} ";
            @base += string.IsNullOrWhiteSpace(this.Scene) ? "" : $"scene: {this.Scene} ";
            @base += this.Colour.ToString();
            return @base;
        }
    }
}

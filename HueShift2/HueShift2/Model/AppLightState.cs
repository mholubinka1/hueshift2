using HueShift2.Model;
using Q42.HueApi;
using System;
using System.Collections.Generic;
using System.Text;

namespace HueShift2.Model
{
    public class AppLightState : IEquatable<AppLightState>
    {
        public byte? Brightness { get; init; }
        public Colour Colour { get; init; }

        public AppLightState()
        {
        }

        public AppLightState(State state)
        {
            this.Brightness = state.Brightness;
            this.Colour = new Colour(state);
        }

        public AppLightState(Colour colour)
        {
            this.Colour = colour;
        }

        public AppLightState(byte? brightness, int colourTemperature)
        {
            this.Brightness = brightness;
            this.Colour = new Colour(colourTemperature);
        }

        internal AppLightState WithBrightness(byte? brightness) => new AppLightState { Brightness = brightness, Colour = this.Colour };
        internal AppLightState WithColour(Colour colour) => new AppLightState { Brightness = this.Brightness, Colour = colour };

        public bool Equals(AppLightState other)
        {
            if (other == null) return false;
            var brightness = other.Brightness == null || other.Brightness == this.Brightness;
            if (this.Colour == null) return brightness && other.Colour == null;
            if (other.Colour == null) return false;
            return brightness && this.Colour.Equals(other.Colour);
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as AppLightState);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Brightness, Colour);
        }

        public override string ToString()
        {
            var @base = this.Brightness == null ? "" : $"brightness: {this.Brightness} ";
            @base += this.Colour.ToString();
            return @base;
        }
    }
}

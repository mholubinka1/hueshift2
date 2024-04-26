using HueShift2.Interfaces;
using HueShift2.Model;
using Q42.HueApi;
using System;
using System.Collections.Generic;
using System.Text;

namespace HueShift2.Model
{
    public class AppLightState : IDeepCloneable<AppLightState> , IEquatable<AppLightState> 
    {
        public byte? Brightness { get; set; }
        public Colour Colour { get; set; }

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
            this.Colour = colour.DeepClone();
        }

        public AppLightState(byte? brightness, int colourTemperature)
        {
            this.Brightness = brightness;
            this.Colour = new Colour(colourTemperature);
        }

        public AppLightState DeepClone()
        {
            return new AppLightState
            {
                Brightness = this.Brightness,
                Colour = this.Colour.DeepClone()
            };
        }

        public bool Equals(AppLightState other)
        {
            var brightness = other.Brightness == null || other.Brightness == this.Brightness;
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

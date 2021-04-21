using HueShift2.Helpers;
using HueShift2.Interfaces;
using Q42.HueApi;
using System;
using System.Collections.Generic;
using System.Text;

namespace HueShift2.Model
{
    public class Colour : IDeepCloneable<Colour> , IEquatable<Colour>
    {
        public ColourMode Mode { get; set; }
        public double[] ColourCoordinates { get; set; }
        public int? ColourTemperature { get; set; }
        public int? Hue { get; set; }
        public int? Saturation { get; set; }

        public Colour()
        {

        }

        public Colour(double[] colourCoordinates)
        {
            this.Mode = ColourMode.XY;
            this.ColourCoordinates = colourCoordinates;
        }

        public Colour(int? colourTemperature)
        {
            this.Mode = ColourMode.CT;
            this.ColourTemperature = colourTemperature;
        }

        public Colour(int? hue, int? saturation)
        {
            this.Mode = ColourMode.Other;
            this.Hue = hue;
            this.Saturation = saturation;
        }

        public Colour(State state)
        {
            this.Mode = state.ColorMode.ToColourMode();
            this.ColourCoordinates = state.ColorCoordinates.DeepClone();
            this.ColourTemperature = state.ColorTemperature;
            this.Hue = state.Hue;
            this.Saturation = state.Saturation;
        }

        public Colour(Colour other)
        {
            this.ColourTemperature = other.ColourTemperature;
            this.Hue = other.Hue;
            this.Saturation = other.Saturation;
        }

        public Colour DeepClone()
        {
            return new Colour
            {
                Mode = this.Mode,
                ColourCoordinates = this.ColourCoordinates.DeepClone(),
                ColourTemperature = this.ColourTemperature,
                Hue = this.Hue,
                Saturation = this.Saturation,
            };
        }

        public bool Equals(Colour other)
        {
            if (this.Mode == other.Mode)
            {
                switch (this.Mode)
                {
                    case ColourMode.XY:
                        return ExtensionMethods.ArrayEquals(this.ColourCoordinates, other.ColourCoordinates);
                    case ColourMode.CT:
                        return this.ColourTemperature == other.ColourTemperature;
                    default:
                        //return this.Hue == lightState.Hue && this.Saturation == lightState.Saturation;
                        throw new NotImplementedException();
                }
            }
            else
            {
                if(ExtensionMethods.ArrayEquals(this.ColourCoordinates, other.ColourCoordinates))
                {
                    return true;
                }
                throw new NotImplementedException();
            }
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as AppLightState);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Mode, ColourCoordinates, ColourTemperature, Hue, Saturation);
        }

        public override string ToString()
        {
            var @base = $"mode: {this.Mode}";
            @base += this.ColourCoordinates == null ? "" : $" xy: [{string.Join(",", this.ColourCoordinates)}]";
            @base += this.ColourTemperature == null ? "" : $" ct: {this.ColourTemperature}";
            @base += this.Hue == null ? "" : $" hue: {this.Hue}";
            @base += this.Saturation == null ? "" : $" sat: {this.Saturation}";
            return @base;
        }
    }
}

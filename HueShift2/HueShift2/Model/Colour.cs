using HueShift2.Helpers;
using Q42.HueApi;
using System;
using System.Collections.Generic;
using System.Text;

namespace HueShift2.Model
{
    public class Colour : IEquatable<Colour>
    {
        public ColourMode Mode { get; init; }
        public double[] ColourCoordinates { get; init; }
        public int? ColourTemperature { get; init; }
        public int? Hue { get; init; }
        public int? Saturation { get; init; }

        public Colour()
        {
        }

        public Colour(double[] colourCoordinates)
        {
            this.Mode = ColourMode.XY;
            this.ColourCoordinates = colourCoordinates.DeepClone();
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

        public bool Equals(Colour other)
        {
            if (other == null) return false;
            if (this.Mode != other.Mode) return false;
            return this.Mode switch
            {
                ColourMode.XY => ExtensionMethods.ArrayEquals(this.ColourCoordinates, other.ColourCoordinates),
                ColourMode.CT => this.ColourTemperature == other.ColourTemperature,
                _ => this.Hue == other.Hue && this.Saturation == other.Saturation,
            };
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as Colour);
        }

        public override int GetHashCode()
        {
            var hc = new HashCode();
            hc.Add(Mode);
            switch (Mode)
            {
                case ColourMode.XY:
                    if (ColourCoordinates != null)
                        foreach (var v in ColourCoordinates)
                            hc.Add(Math.Round(v, 7));
                    break;
                case ColourMode.CT:
                    hc.Add(ColourTemperature);
                    break;
                default:
                    hc.Add(Hue);
                    hc.Add(Saturation);
                    break;
            }
            return hc.ToHashCode();
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

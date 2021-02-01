﻿using HueShift2.Helpers;
using Q42.HueApi;
using System;
using System.Collections.Generic;
using System.Text;

namespace HueShift2.Model
{
    public class Colour
    {
        public double[] ColourCoordinates { get; private set; }
        public int? ColourTemperature { get; private set; }
        public int? Hue { get; private set; }
        public int? Saturation { get; private set; }

        public Colour(double[] colourCoordinates)
        {
            this.ColourCoordinates = colourCoordinates;
        }

        public Colour(int? colourTemperature)
        {
            this.ColourTemperature = colourTemperature;
        }

        public Colour(int? hue, int? saturation)
        {
            this.Hue = hue;
            this.Saturation = saturation;
        }

        public Colour(State state)
        {
            this.ColourCoordinates = state.ColorCoordinates;
            this.ColourTemperature = state.ColorTemperature;
            this.Hue = state.Hue;
            this.Saturation = state.Saturation;
        }

        private void Clear()
        {
            ColourCoordinates = null;
            ColourTemperature = null;
            Hue = null;
            Saturation = null;
        }

        public void ExecuteCommand(LightCommand command)
        {
            this.Clear();
            if (command.ColorCoordinates != null)
            {
                this.ColourCoordinates = command.ColorCoordinates;
                return;
            }
            if (command.ColorTemperature != null)
            {
                this.ColourTemperature = command.ColorTemperature;
                return;
            }
            if (command.Hue != null && command.Saturation != null)
            {
                this.Hue = command.Hue;
                this.Saturation = command.Saturation;
            }
            throw new InvalidOperationException();
        }

        public bool Matches(State lightState)
        {
            switch(lightState.ColorMode)
            {
                case "xy":
                    return ExtensionMethods.ArrayEquals(this.ColourCoordinates, lightState.ColorCoordinates);
                case "ct":
                    return this.ColourTemperature == lightState.ColorTemperature;
                default:
                    //return this.Hue == lightState.Hue && this.Saturation == lightState.Saturation;
                    throw new NotImplementedException();
            }
        }
    }
}
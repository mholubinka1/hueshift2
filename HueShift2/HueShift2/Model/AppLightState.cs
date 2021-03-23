using HueShift2.Configuration.Model;
using HueShift2.Model;
using Q42.HueApi;
using System;
using System.Collections.Generic;
using System.Text;

namespace HueShift2
{
    public class AppLightState
    {
        public byte Brightness { get; private set; }
        public string Scene { get; private set; }
        public Colour Colour { get; private set; }

        public AppLightState(Colour colour)
        {
            this.Colour = colour;
        }

        public AppLightState(State state)
        {
            this.Brightness = state.Brightness;
            this.Colour = new Colour(state);
        }

        public void Refresh(DateTime currentTime, bool isOn)
        {

        }

        public void ExecuteTransitionCommand(LightCommand command, DateTime currentTime)
        {
            if(command.TransitionTime != null)
            {
                this.PowerState = LightPowerState.Transitioning;
                this.Transition = new Transition(currentTime, (TimeSpan)command.TransitionTime);
            }
            else
            {
                this.PowerState = LightPowerState.On;
            }
            if(command.Brightness != null)
            {
                this.Brightness = (byte)command.Brightness;
            }
            this.Colour.ExecuteCommand(command);
        }

        public void ExecuteInstantaneousCommand(LightCommand command)
        {
            if (command.Brightness != null)
            {
                this.Brightness = (byte)command.Brightness;
            }
            this.Colour.ExecuteCommand(command);
        }

        public bool Matches(State lightState)
        {
            return this.Colour.Matches(lightState);
        }

        public string ToString(bool targetState)
        {
            var @base = $"Brightness: {this.Brightness} Scene: {this.Scene} Colour: ["+ this.Colour.ToString() +"]";
            if (targetState)
            {
                return @base;
            }
            var returnString = $"Power: {this.PowerState.ToString()} " + @base;
            if (this.PowerState == LightPowerState.Transitioning)
            {
                returnString += $" Transition: [" + this.Transition.ToString() + "]";
            }
            return returnString;
            
        }
    }
}

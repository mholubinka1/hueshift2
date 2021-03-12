using HueShift2.Configuration.Model;
using HueShift2.Model;
using Q42.HueApi;
using System;
using System.Collections.Generic;
using System.Text;

namespace HueShift2
{
    public class HueShiftLightState
    {
        public LightPowerState PowerState { get; private set; }
        public byte Brightness { get; private set; }
        public string Scene { get; private set; }
        public Colour Colour { get; private set; }

        public Transition Transition;

        public HueShiftLightState(Colour colour)
        {
            this.Colour = colour;
        }

        public HueShiftLightState(State state)
        {
            this.PowerState = state.On.ToPowerState();
            this.Brightness = state.Brightness;
            this.Colour = new Colour(state);
        }

        public void Refresh(DateTime currentTime, bool isOn)
        {
            if (isOn)
            {
                if (this.PowerState == LightPowerState.Transitioning)
                {
                    if (this.Transition == null) throw new NullReferenceException();
                    if (this.Transition.IsExpired(currentTime))
                    {
                        this.PowerState = LightPowerState.On;
                        this.Transition = null;
                    }
                }
                else
                {
                    this.PowerState = LightPowerState.On;
                    this.Transition = null;
                }
            }
            else
            {
                this.PowerState = LightPowerState.Off;
                this.Transition = null;
            }
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

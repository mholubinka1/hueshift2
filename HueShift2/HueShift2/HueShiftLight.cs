using HueShift2.Model;
using Q42.HueApi;
using System;
using System.Collections.Generic;
using System.Text;

namespace HueShift2
{
    public class HueShiftLight
    {
        public readonly string Id;
        public LightControlState ControlState {get; set;}
        public HueShiftLightState State { get; private set; }

        public HueShiftLight(Light light)
        {
            Id = light.Id;
            ControlState = LightControlState.HueShift;
            State = new HueShiftLightState(light.State);
        }

        public void Refresh(Light light, DateTime currentTime)
        {
            var on = light.State.On;
            //TODO: 1. turning lights "to expected on state" turns brightness up to 100%
            //TODO: 2. turning on the lights "to a new state - different brightness and/or colour state" sets lights to Hybrid Manual
            if (on)
            {
                if (this.ControlState == LightControlState.HueShift)
                {
                    if (!this.State.Matches(light.State) && this.State.PowerState != LightPowerState.Transitioning)
                    {
                        this.ControlState = LightControlState.Manual;
                    }
                }
                if (this.ControlState == LightControlState.Manual)
                {
                    if (this.State.PowerState == LightPowerState.Off)
                    {
                        this.ControlState = LightControlState.HueShift;
                    }
                }
            }
            this.State.Refresh(currentTime, on);
        }

        public void TakeControl()
        {
            this.ControlState = LightControlState.HueShift;
        }

        public void Exclude()
        {
            this.ControlState = LightControlState.Excluded;
        }

        #region Execute Transitions

        public void ExecuteTransitionCommand(LightCommand command, DateTime currentTime)
        {
            if (this.ControlState != LightControlState.HueShift) throw new InvalidOperationException();
            this.State.ExecuteTransitionCommand(command, currentTime);
        }

        public void ExecuteInstantaneousCommand(LightCommand command)
        {
            this.State.ExecuteInstantaneousCommand(command);
        }

        #endregion
    }
}

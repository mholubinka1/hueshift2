using Q42.HueApi;
using System;
using System.Collections.Generic;
using System.Text;

namespace HueShift2.Model
{
    public class HueShiftLight
    {
        public readonly string Id;

        public LightControlState ControlState {get; set;}
        public LightState State { get; private set; }

        public HueShiftLight(Light light)
        {
            Id = light.Id;
            ControlState = LightControlState.HueShift;
            State = new LightState(light.State);
        }

        public void ExecuteTransitionCommand(LightCommand command, DateTime currentTime)
        {
            if(this.ControlState != LightControlState.HueShift) throw new InvalidOperationException();
            this.State.ExecuteTransitionCommand(command, currentTime);
        }

        public void ExecuteInstantaneousCommand(LightCommand command)
        {
            this.State.ExecuteInstantaneousCommand(command);
        }

        public void Refresh(Light light, DateTime currentTime)
        {
            var isOn = light.State.On;
            //FIXME: this needs testing
            if (isOn)
            {
                if (this.ControlState == LightControlState.HueShift)
                {
                    if (!this.State.Matches(light.State))
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
            this.State.Refresh(currentTime, isOn);
        }

        public void TakeControl()
        {
            this.ControlState = LightControlState.HueShift;
        }

        public void Exclude()
        {
            this.ControlState = LightControlState.Excluded;
        }
    }
}

using HueShift2.Model;
using Q42.HueApi;
using System;
using System.Collections.Generic;
using System.Text;

namespace HueShift2.Model
{
    public class AppLight
    {
        public AppLightState State { get; set; }

        public AppLight(Light light)
        {
            State = new AppLightState(light.State);
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

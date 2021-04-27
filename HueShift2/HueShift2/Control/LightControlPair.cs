using HueShift2.Helpers;
using HueShift2.Logging;
using HueShift2.Model;
using Q42.HueApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HueShift2.Control
{
    public class LightControlPair
    {
        public LightProperties Properties { get; private set; }
        public LightPowerState PowerState { get; private set; }
        public LightControlState AppControlState { get; private set; }
        public State NetworkLight { get; private set; }
        public AppLightState ExpectedLight { get; private set; }
        public Transition Transition { get; private set; }
        public bool ResetOccurred { get; private set; }

        public LightControlPair(Light networkLight)
        {
            this.Properties = new LightProperties(networkLight);
            this.PowerState = networkLight.State.On ? LightPowerState.On : LightPowerState.Off;
            this.AppControlState = LightControlState.HueShift;
            this.NetworkLight = networkLight.State;
            this.ExpectedLight = new AppLightState(this.NetworkLight);
            this.ResetOccurred = false;
        }

        public void RefreshTransition(bool isOn, DateTime currentTime)
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
                }
            }
            else
            {
                this.PowerState = LightPowerState.Off;
                this.Transition = null;
            }
        }

        public void Refresh(State networkLight, DateTime currentTime)
        {
            this.NetworkLight = networkLight;
            this.ExpectedLight.Brightness = this.NetworkLight.Brightness;
            var isOn = NetworkLight.On;
            if (isOn)
            {
                switch (this.AppControlState)
                {
                    case LightControlState.HueShift:
                        if (!this.NetworkLight.ColourEquals(this.ExpectedLight) && this.PowerState == LightPowerState.On)
                        {
                            this.AppControlState = LightControlState.Manual;
                        }
                        if (this.NetworkLight.ColourEquals(this.ExpectedLight) && this.PowerState == LightPowerState.Syncing)
                        {
                            this.PowerState = LightPowerState.On;
                        }
                        break;
                    case LightControlState.Manual:
                        if (this.PowerState == LightPowerState.Off)
                        {
                            this.AppControlState = LightControlState.HueShift;
                        }
                        break;
                    case LightControlState.Excluded:
                        break;
                }
            }
            RefreshTransition(isOn, currentTime);
        }

        public bool RequiresSync(out LightCommand syncCommand)
        {
            syncCommand = null;
            if (this.PowerState != LightPowerState.On || this.AppControlState != LightControlState.HueShift)
            {
                return false;
            }
            if (this.NetworkLight.ColourEquals(this.ExpectedLight))
            {
                if (this.ResetOccurred)
                {
                    var brightness = (byte)254;
                    this.ResetOccurred = false;
                    if (this.NetworkLight.Brightness != brightness)
                    {
                        syncCommand = new LightCommand { Brightness = brightness };
                        return true;
                    }
                }
                return false;
            }
            syncCommand = this.ExpectedLight.ToCommand();
            return true;
        }

        public void Reset()
        {
            if(this.AppControlState == LightControlState.Manual)
            {
                this.AppControlState = LightControlState.HueShift;
            }
            this.ResetOccurred = true;
        }

        private void ClearColourState()
        {
            this.ExpectedLight.Colour.Mode = ColourMode.None;
            this.ExpectedLight.Colour.ColourCoordinates = null;
            this.ExpectedLight.Colour.ColourTemperature = null;
            this.ExpectedLight.Colour.Hue = null;
            this.ExpectedLight.Colour.Saturation = null;
        }

        private void ChangeColour(LightCommand command)
        {
            ClearColourState();
            if (command.ColorCoordinates != null)
            {
                this.ExpectedLight.Colour.Mode = ColourMode.XY;
                this.ExpectedLight.Colour.ColourCoordinates = command.ColorCoordinates;
                return;
            }
            else if (command.ColorTemperature != null)
            {
                this.ExpectedLight.Colour.Mode = ColourMode.CT;
                this.ExpectedLight.Colour.ColourTemperature = command.ColorTemperature;
                return;
            }
            else if (command.Hue != null && command.Saturation != null)
            {
                this.ExpectedLight.Colour.Mode = ColourMode.Other;
                this.ExpectedLight.Colour.Hue = command.Hue;
                this.ExpectedLight.Colour.Saturation = command.Saturation;
            }
            this.ExpectedLight.Colour.Mode = ColourMode.None;
            throw new InvalidOperationException();
        }

        public void ExecuteCommand(LightCommand command, DateTime currentTime)
        {
            if (this.AppControlState != LightControlState.HueShift) throw new InvalidOperationException();
            if (command.TransitionTime != null)
            {
                this.PowerState = LightPowerState.Transitioning;
                var asyncTolerance = 5;
                this.Transition = new Transition(currentTime, 
                    ((TimeSpan)command.TransitionTime).Add(new TimeSpan(0,0, asyncTolerance)));
            }
            else
            {
                this.PowerState = LightPowerState.On;
            }
            ExecuteInstantaneousCommand(command);
        }

        public void ExecuteInstantaneousCommand(LightCommand command)
        {
            if (command.Brightness != null)
            {
                this.ExpectedLight.Brightness = (byte)command.Brightness;
            }
            else
            {
                this.ExpectedLight.Brightness = this.NetworkLight.Brightness;
            }
            ChangeColour(command);
        }

        public void ExecuteSync()
        {
            this.PowerState = LightPowerState.Syncing;
        }

        public void Exclude(bool isExcluded)
        {
            if (isExcluded)
            {
                AppControlState = LightControlState.Excluded;
            }
            else
            {
                if (this.AppControlState == LightControlState.Excluded)
                {
                    this.AppControlState = LightControlState.HueShift;
                }
            }
        }

        public override string ToString()
        {
            var @base = $"Control Pair | Id: {this.Properties.Id} Name: {this.Properties.Name} | {this.PowerState} - Control: {this.AppControlState}";
            if (this.PowerState == LightPowerState.Transitioning)
            {
                var remaining = (DateTime.Now - (this.Transition.StartedTime + this.Transition.Duration)).TotalSeconds;
                @base += $" | Transition Time Remaining: {remaining}s\n";
            }
            else
            {
                @base += "\n";
            }
            var networkLight = $"Network Light | " + this.NetworkLight.ToLogString() + "\n";
            var expectedLight = $"Expected Light | " + this.ExpectedLight.ToString();
            return @base + networkLight + expectedLight;
        }
    }
}

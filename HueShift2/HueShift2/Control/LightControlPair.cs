using HueShift2.Helpers;
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

        private bool resetOccurred;

        public LightControlPair(Light networkLight)
        {
            this.Properties = new LightProperties(networkLight);
            this.PowerState = networkLight.State.On ? LightPowerState.On : LightPowerState.Off;
            this.AppControlState = LightControlState.HueShift;
            this.NetworkLight = networkLight.State;
            this.ExpectedLight = new AppLightState(this.NetworkLight);
            this.resetOccurred = false;
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
            RefreshTransition(isOn, currentTime);
            if (isOn)
            {
                switch (this.AppControlState)
                {
                    case LightControlState.HueShift:
                        if (this.NetworkLight.ColourEquals(this.ExpectedLight) && this.PowerState != LightPowerState.Transitioning)
                        {
                            this.AppControlState = LightControlState.Manual;
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
                if (this.resetOccurred)
                {
                    var brightness = (byte)254;
                    this.resetOccurred = false;
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
            this.resetOccurred = true;
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
                this.Transition = new Transition(currentTime, (TimeSpan)command.TransitionTime);
            }
            else
            {
                this.PowerState = LightPowerState.On;
            }
            if (command.Brightness != null)
            {
                this.ExpectedLight.Brightness = (byte)command.Brightness;
            }
            ChangeColour(command);
        }

        public void ExecuteInstantaneousCommand(LightCommand command)
        {
            if (command.Brightness != null)
            {
                this.ExpectedLight.Brightness = (byte)command.Brightness;
            }
            ChangeColour(command);
        }

        #region Exclusion

        public void Exclude()
        {
            AppControlState = LightControlState.Excluded;
        }

        public void Unexclude()
        {
            if(this.AppControlState == LightControlState.Excluded)
            {
                this.AppControlState = LightControlState.HueShift;
            }
        }

        #endregion
    }
}

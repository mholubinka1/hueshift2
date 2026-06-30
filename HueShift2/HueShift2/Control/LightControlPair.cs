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
        public bool SyncRequired { get; private set; }
        public bool SyncConfirmed { get; private set; }
        public bool SyncFailed { get; private set; }

        private DateTime? syncIssuedAt;

        public LightControlPair(Light networkLight)
        {
            this.Properties = new LightProperties(networkLight);
            this.PowerState = networkLight.State.DeterminePowerState();
            this.AppControlState = LightControlState.HueShift;
            this.NetworkLight = networkLight.State;
            this.ExpectedLight = new AppLightState(this.NetworkLight);
            this.ResetOccurred = false;
            this.SyncRequired = false;
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
                else if (this.PowerState != LightPowerState.Syncing)
                {
                    this.PowerState = LightPowerState.On;
                }
            }
            else
            {
                this.PowerState = LightPowerState.Off;
                this.Transition = null;
                this.syncIssuedAt = null;
                this.SyncRequired = false;
            }
        }

        public void Refresh(State networkLight, DateTime currentTime, int minCt, int maxCt, TimeSpan syncGracePeriod)
        {
            this.NetworkLight = networkLight;
            this.ExpectedLight.Brightness = this.NetworkLight.Brightness;
            this.SyncConfirmed = false;
            this.SyncFailed = false;
            var isOn = networkLight.DeterminePowerState() == LightPowerState.On;
            var wasOff = this.PowerState == LightPowerState.Off;
            if (isOn)
            {
                switch (this.AppControlState)
                {
                    case LightControlState.HueShift:
                        if (this.PowerState == LightPowerState.Syncing)
                        {
                            if (this.NetworkLight.Equals(this.ExpectedLight, minCt, maxCt))
                            {
                                this.PowerState = LightPowerState.On;
                                this.Transition = null;
                                this.syncIssuedAt = null;
                                this.SyncRequired = false;
                                this.SyncConfirmed = true;
                            }
                            else if (this.Transition.IsExpired(currentTime))
                            {
                                var withinGrace = this.syncIssuedAt.HasValue && (currentTime - this.syncIssuedAt.Value) < syncGracePeriod;
                                if (withinGrace)
                                {
                                    this.SyncRequired = true;
                                }
                                else
                                {
                                    this.PowerState = LightPowerState.On;
                                    this.Transition = null;
                                    this.syncIssuedAt = null;
                                    this.SyncRequired = false;
                                    this.AppControlState = LightControlState.Manual;
                                    this.SyncFailed = true;
                                }
                            }
                        }
                        else if (!this.NetworkLight.Equals(this.ExpectedLight, minCt, maxCt) && this.PowerState == LightPowerState.On)
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
            RefreshTransition(isOn, currentTime);
            if (isOn && wasOff && this.PowerState == LightPowerState.On && this.AppControlState == LightControlState.HueShift)
            {
                this.SyncRequired = true;
            }
        }

        public bool RequiresSync(out LightCommand syncCommand)
        {
            syncCommand = null;
            if (this.PowerState != LightPowerState.On || this.AppControlState != LightControlState.HueShift)
                return false;

            if (this.SyncRequired)
            {
                this.SyncRequired = false;
                syncCommand = this.ExpectedLight.ToCommand();
                if (this.ResetOccurred)
                {
                    this.ResetOccurred = false;
                    syncCommand.Brightness = 254;
                }
                return true;
            }

            if (this.ResetOccurred)
            {
                var brightness = (byte)254;
                this.ResetOccurred = false;
                if (this.NetworkLight.Brightness != brightness)
                {
                    syncCommand = this.ExpectedLight.ToCommand();
                    syncCommand.Brightness = brightness;
                    return true;
                }
            }

            return false;
        }

        public bool RequiresRetrySync(out LightCommand syncCommand)
        {
            syncCommand = null;
            if (this.PowerState != LightPowerState.Syncing || !this.SyncRequired)
                return false;
            this.SyncRequired = false;
            syncCommand = this.ExpectedLight.ToCommand();
            return true;
        }

        public void Reset()
        {
            if (this.AppControlState == LightControlState.Manual)
            {
                this.AppControlState = LightControlState.HueShift;
                this.SyncRequired = true;
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
            if (command.ColorTemperature != null)
            {
                this.ExpectedLight.Colour.Mode = ColourMode.CT;
                this.ExpectedLight.Colour.ColourTemperature = command.ColorTemperature;
                return;
            }
            // HS or unknown: ClearColourState already set Mode = None; nothing further needed
        }

        public void ExecuteCommand(LightCommand command, DateTime currentTime, TransitionType transitionType)
        {
            if (this.AppControlState != LightControlState.HueShift) throw new InvalidOperationException();
            if (command.TransitionTime != null)
            {
                this.PowerState = LightPowerState.Transitioning;
                this.Transition = new Transition(currentTime, (TimeSpan)command.TransitionTime, transitionType);
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

        public void ExecuteSync(TimeSpan duration, DateTime currentTime)
        {
            this.PowerState = LightPowerState.Syncing;
            this.Transition = new Transition(currentTime, duration, TransitionType.Sync);
            this.syncIssuedAt ??= currentTime;
        }

        public override string ToString()
        {
            var @base = $"Control Pair | Id: {this.Properties.Id} Name: {this.Properties.Name} | {this.PowerState} - Control: {this.AppControlState}";
            if (this.PowerState == LightPowerState.Transitioning || this.PowerState == LightPowerState.Syncing)
            {
                @base += $" | Transition Time Remaining: {this.Transition.SecondsRemaining}s";
            }
            var networkLight = $" | Network Light | " + this.NetworkLight.ToLogString();
            var expectedLight = $" | Expected Light | " + this.ExpectedLight.ToString();
            return @base + networkLight + expectedLight;
        }
    }
}

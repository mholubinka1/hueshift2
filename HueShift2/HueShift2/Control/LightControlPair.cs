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
        private const int TransitionSettlingPeriodSeconds = 10;
        private const byte MaxBrightness = 254;

        private bool _isOn;
        private bool _isReachable;
        private bool _isTransitioning;

        public LightProperties Properties { get; private set; }
        public LightControlState AppControlState { get; private set; }
        public State NetworkLight { get; private set; }
        public AppLightState ExpectedLight { get; private set; }
        private Transition Transition { get; set; }
        public bool ResetOccurred { get; private set; }
        public bool SyncRequired { get; private set; }

        public bool IsOn() => _isOn;
        public bool IsReachable() => _isReachable;
        public bool IsTransitioning() => _isTransitioning;
        public bool CanReceiveCommand() => AppControlState == LightControlState.HueShiftControlled && _isOn && !_isTransitioning && _isReachable;
        public double? TransitionSecondsRemaining() => _isTransitioning ? Transition?.SecondsRemaining : null;

        public LightControlPair(Light networkLight)
        {
            this.Properties = new LightProperties(networkLight);
            _isReachable = networkLight.State.IsReachable == true;
            _isOn = _isReachable && networkLight.State.On;
            _isTransitioning = false;
            this.AppControlState = LightControlState.HueShiftControlled;
            this.NetworkLight = networkLight.State;
            this.ExpectedLight = new AppLightState(this.NetworkLight);
            this.ResetOccurred = false;
            this.SyncRequired = false;
        }

        public void Refresh(State networkLight, DateTime currentTime, int minCt, int maxCt)
        {
            this.NetworkLight = networkLight;

            var prevIsOn = _isOn;
            var prevIsReachable = _isReachable;

            _isReachable = networkLight.IsReachable == true;
            if (_isReachable)
            {
                _isOn = networkLight.On;
                this.ExpectedLight.Brightness = this.NetworkLight.Brightness;
            }

            if (_isReachable && _isOn && !_isTransitioning)
            {
                switch (this.AppControlState)
                {
                    case LightControlState.HueShiftControlled:
                        if (prevIsOn && prevIsReachable)
                        {
                            if (IsManualOverride(minCt, maxCt))
                                TakeManualOverride();
                            else if (HasDrifted())
                                this.SyncRequired = true;
                        }
                        break;
                    case LightControlState.Manual:
                        if (!prevIsOn && prevIsReachable)
                            ReturnToControl();
                        break;
                    case LightControlState.Excluded:
                        break;
                }
            }

            RefreshTransition(currentTime);

            if (_isReachable && _isOn && this.AppControlState == LightControlState.HueShiftControlled)
            {
                var justTurnedOn = !prevIsOn && prevIsReachable;
                var justReconnected = !prevIsReachable;
                if (justTurnedOn || justReconnected)
                    this.SyncRequired = true;
            }

            if (_isReachable && !_isOn)
                this.SyncRequired = false;
        }

        public bool RequiresSync(out LightCommand syncCommand)
        {
            syncCommand = null;
            if (!CanReceiveCommand()) return false;

            if (this.SyncRequired)
            {
                this.SyncRequired = false;
                syncCommand = this.ExpectedLight.ToCommand();
                if (this.ResetOccurred)
                {
                    this.ResetOccurred = false;
                    syncCommand.Brightness = MaxBrightness;
                }
                return true;
            }

            if (this.ResetOccurred)
            {
                var brightness = MaxBrightness;
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

        public void Reset()
        {
            if (this.AppControlState == LightControlState.Excluded) return;
            if (this.AppControlState == LightControlState.Manual)
            {
                ReturnToControl();
                this.SyncRequired = true;
            }
            this.ResetOccurred = true;
        }

        internal void MarkForSync()
        {
            this.SyncRequired = true;
        }

        public void ExecuteCommand(LightCommand command, DateTime currentTime, TransitionType transitionType)
        {
            if (this.AppControlState != LightControlState.HueShiftControlled) throw new InvalidOperationException();
            if (command.TransitionTime != null)
            {
                _isTransitioning = true;
                var internalDuration = (TimeSpan)command.TransitionTime + TimeSpan.FromSeconds(TransitionSettlingPeriodSeconds);
                this.Transition = new Transition(currentTime, internalDuration, transitionType);
            }
            else
            {
                _isTransitioning = false;
            }
            UpdateExpectedState(command);
        }

        public void UpdateExpectedState(LightCommand command)
        {
            if (command.Brightness != null)
                this.ExpectedLight.Brightness = (byte)command.Brightness;
            else if (_isReachable)
                this.ExpectedLight.Brightness = this.NetworkLight.Brightness;
            ChangeColour(command);
        }

        public override string ToString()
        {
            var powerDesc = _isReachable ? (_isOn ? "On" : "Off") : "Unreachable";
            var @base = $"Control Pair | Id: {this.Properties.Id} Name: {this.Properties.Name} | {powerDesc} - Control: {this.AppControlState}";
            if (_isTransitioning)
                @base += $" | Transition Time Remaining: {Transition?.SecondsRemaining}s";
            var networkLight = $" | Network Light | " + this.NetworkLight.ToLogString();
            var expectedLight = $" | Expected Light | " + this.ExpectedLight.ToString();
            return @base + networkLight + expectedLight;
        }

        internal void Exclude()
        {
            this.AppControlState = LightControlState.Excluded;
            this.SyncRequired = false;
            this.ResetOccurred = false;
        }

        internal void Unexclude()
        {
            this.AppControlState = LightControlState.HueShiftControlled;
            this.SyncRequired = true;
        }

        private void TakeManualOverride() => this.AppControlState = LightControlState.Manual;
        private void ReturnToControl() => this.AppControlState = LightControlState.HueShiftControlled;

        private bool IsManualOverride(int minCt, int maxCt)
        {
            var low = Math.Min(minCt, maxCt);
            var high = Math.Max(minCt, maxCt);
            return this.NetworkLight.ColorMode.ToColourMode() switch
            {
                ColourMode.CT => this.NetworkLight.ColorTemperature == null || this.NetworkLight.ColorTemperature.Value < low || this.NetworkLight.ColorTemperature.Value > high,
                ColourMode.XY => this.NetworkLight.ColorCoordinates == null || !Helpers.ExtensionMethods.TryXyToCt(this.NetworkLight.ColorCoordinates, out var convertedCt) || convertedCt < low || convertedCt > high,
                _ => true,
            };
        }

        private bool HasDrifted()
        {
            if (this.ExpectedLight.Colour.ColourTemperature == null) return false;
            var expectedCt = this.ExpectedLight.Colour.ColourTemperature.Value;
            return this.NetworkLight.ColorMode.ToColourMode() switch
            {
                ColourMode.CT => this.NetworkLight.ColorTemperature != null && Math.Abs(this.NetworkLight.ColorTemperature.Value - expectedCt) > 10,
                ColourMode.XY => this.NetworkLight.ColorCoordinates != null && Helpers.ExtensionMethods.TryXyToCt(this.NetworkLight.ColorCoordinates, out var convertedCt) && Math.Abs(convertedCt - expectedCt) > 10,
                _ => false,
            };
        }

        private void RefreshTransition(DateTime currentTime)
        {
            if (!_isReachable || !_isOn)
            {
                if (_isReachable)
                {
                    _isTransitioning = false;
                    this.Transition = null;
                }
                else if (_isTransitioning && (this.Transition?.IsExpired(currentTime) ?? false))
                {
                    _isTransitioning = false;
                    this.Transition = null;
                }
                return;
            }
            if (_isTransitioning)
            {
                if (this.Transition == null) throw new NullReferenceException();
                if (this.Transition.IsExpired(currentTime))
                {
                    _isTransitioning = false;
                    this.Transition = null;
                }
            }
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
        }
    }
}

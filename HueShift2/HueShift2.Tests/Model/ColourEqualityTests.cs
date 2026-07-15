using HueShift2.Model;
using Xunit;

namespace HueShift2.Tests.Model
{
    public class ColourEqualityTests
    {
        [Fact]
        public void SameCtColours_AreEqual()
        {
            // Given: two CT Colour instances with the same colour temperature
            var a = new Colour(300);
            var b = new Colour(300);

            // When / Then: equal via the typed overload
            Assert.True(a.Equals(b));
        }

        [Fact]
        public void SameCtColour_EqualViaObjectOverload()
        {
            // Given: two CT Colour instances with identical colour temperature
            var a = new Colour(300);
            object b = new Colour(300);

            // When: compared via the object overload
            // Then: equal (previously caused infinite recursion due to cast to AppLightState)
            Assert.True(a.Equals(b));
        }

        [Fact]
        public void NoneModeColours_AreEqual()
        {
            // Given: two Colour instances with no mode set (both Mode=None, all properties null)
            var a = new Colour();
            var b = new Colour();

            // When / Then: equal — two "no colour" states are the same state
            Assert.True(a.Equals(b));
        }

        [Fact]
        public void DifferentModeColours_AreUnequalWithoutThrowing()
        {
            // Given: one CT Colour and one XY Colour
            var ct = new Colour(300);
            var xy = new Colour(new[] { 0.3, 0.3 });

            // When / Then: comparison returns false, does not throw
            Assert.False(ct.Equals(xy));
        }

        [Fact]
        public void SameXyCoordinates_AreEqualByValue()
        {
            // Given: two XY Colour instances constructed from different double[] arrays with the same values
            var a = new Colour(new[] { 0.3127, 0.3290 });
            var b = new Colour(new[] { 0.3127, 0.3290 });

            // When / Then: equal by value (ArrayEquals, not reference equality)
            Assert.True(a.Equals(b));
        }

        [Fact]
        public void DifferentXyCoordinates_AreUnequal()
        {
            // Given: two XY Colour instances with different coordinate values
            var a = new Colour(new[] { 0.3127, 0.3290 });
            var b = new Colour(new[] { 0.4500, 0.4000 });

            // When / Then: unequal
            Assert.False(a.Equals(b));
        }

        [Fact]
        public void CtColoursWithDifferentTemperatures_AreUnequal()
        {
            // Given: two CT Colour instances with different colour temperatures
            var a = new Colour(250);
            var b = new Colour(454);

            // When / Then: unequal
            Assert.False(a.Equals(b));
        }

        [Fact]
        public void NullColour_IsNotEqualToCtColour()
        {
            // Given: a CT Colour and null
            var a = new Colour(300);
            Colour? nullColour = null;

            // When / Then: returns false, does not throw
            Assert.False(a.Equals(nullColour));
            Assert.False(a.Equals((object?)nullColour));
        }

        [Fact]
        public void XyModeWithNullCoordinates_AreEqualToEachOther()
        {
            // Given: two XY-mode Colours constructed via object initializer with null ColourCoordinates
            var a = new Colour { Mode = ColourMode.XY, ColourCoordinates = null };
            var b = new Colour { Mode = ColourMode.XY, ColourCoordinates = null };

            // When / Then: ArrayEquals(null, null) returns true — null coordinates compare equal
            Assert.True(a.Equals(b));
        }

        [Fact]
        public void XyModeWithNullCoordinates_IsNotEqualToXyModeWithCoordinates()
        {
            // Given: an XY-mode Colour with null coordinates and one with real coordinates
            var a = new Colour { Mode = ColourMode.XY, ColourCoordinates = null };
            var b = new Colour(new[] { 0.3127, 0.3290 });

            // When / Then: ArrayEquals(null, non-null) returns false
            Assert.False(a.Equals(b));
        }

        [Fact]
        public void EqualNoneModeColours_HaveEqualHashCodes()
        {
            // Given: two equal None-mode Colours
            var a = new Colour();
            var b = new Colour();

            // When / Then: equal objects must have equal hash codes
            Assert.True(a.Equals(b));
            Assert.Equal(a.GetHashCode(), b.GetHashCode());
        }

        [Fact]
        public void EqualXyColours_HaveEqualHashCodes()
        {
            // Given: two XY Colours with the same coordinates
            var a = new Colour(new[] { 0.3127, 0.3290 });
            var b = new Colour(new[] { 0.3127, 0.3290 });

            // When / Then: equal by value (via ArrayEquals with epsilon) → must also have equal hash codes
            Assert.True(a.Equals(b));
            Assert.Equal(a.GetHashCode(), b.GetHashCode());
        }

        [Fact]
        public void EqualCtColours_HaveEqualHashCodes_EvenWhenCoordinatesDiffer()
        {
            // Given: two CT Colours with the same temperature but different ColourCoordinates
            // (e.g. one from Colour(State) which copies XY coords, one from Colour(int?))
            var fromState = new Colour { Mode = ColourMode.CT, ColourTemperature = 300, ColourCoordinates = new[] { 0.3127, 0.3290 } };
            var fromCt = new Colour(300);

            // When / Then: Equals compares only CT field; GetHashCode must also ignore coordinates for CT mode
            Assert.True(fromState.Equals(fromCt));
            Assert.Equal(fromState.GetHashCode(), fromCt.GetHashCode());
        }

        [Fact]
        public void OtherModeColours_WithSameHueAndSaturation_AreEqual()
        {
            // Given: two ColourMode.Other Colours with identical Hue and Saturation
            var a = new Colour(hue: 120, saturation: 254);
            var b = new Colour(hue: 120, saturation: 254);

            // When / Then: equal by Hue and Saturation (spec: ColourMode.Other compares by Hue and Saturation)
            Assert.True(a.Equals(b));
            Assert.Equal(a.GetHashCode(), b.GetHashCode());
        }

        [Fact]
        public void OtherModeColours_WithDifferentHue_AreUnequal()
        {
            // Given: two ColourMode.Other Colours with the same saturation but different hue
            var a = new Colour(hue: 120, saturation: 254);
            var b = new Colour(hue: 200, saturation: 254);

            // When / Then: unequal
            Assert.False(a.Equals(b));
        }
    }
}

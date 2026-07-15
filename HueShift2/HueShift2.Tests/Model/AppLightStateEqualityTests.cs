using HueShift2.Model;
using Xunit;

namespace HueShift2.Tests.Model
{
    public class AppLightStateEqualityTests
    {
        [Fact]
        public void Equals_WithNull_ReturnsFalse()
        {
            // Given: an AppLightState with a colour
            var state = new AppLightState(new Colour(300));

            // When / Then: comparing to null returns false, does not throw
            AppLightState? nullState = null;
            Assert.False(state.Equals(nullState));
            Assert.False(state.Equals((object?)nullState));
        }

        [Fact]
        public void Equals_SameBrightnessAndColour_ReturnsTrue()
        {
            // Given: two AppLightState instances with identical brightness and CT colour
            var a = new AppLightState { Brightness = 128, Colour = new Colour(300) };
            var b = new AppLightState { Brightness = 128, Colour = new Colour(300) };

            // When / Then: equal
            Assert.True(a.Equals(b));
        }

        [Fact]
        public void Equals_DifferentBrightness_ReturnsFalse()
        {
            // Given: two AppLightState instances with the same colour but different brightness
            var a = new AppLightState { Brightness = 128, Colour = new Colour(300) };
            var b = new AppLightState { Brightness = 200, Colour = new Colour(300) };

            // When / Then: unequal
            Assert.False(a.Equals(b));
        }

        [Fact]
        public void Equals_ViaObjectOverload_ReturnsTrueForEqualStates()
        {
            // Given: two equal AppLightState instances, one boxed as object
            var a = new AppLightState(new Colour(300));
            object b = new AppLightState(new Colour(300));

            // When / Then: Equals(object) delegates to Equals(AppLightState)
            Assert.True(a.Equals(b));
        }

        [Fact]
        public void Equals_WhenThisColourIsNull_ReturnsTrueOnlyIfOtherColourIsAlsoNull()
        {
            // Given: an AppLightState with null Colour (constructed with no-arg ctor)
            var withNullColour = new AppLightState();
            var alsoNullColour = new AppLightState();
            var withColour = new AppLightState(new Colour(300));

            // When / Then: null-colour states are equal to each other, not to a state with a colour
            Assert.True(withNullColour.Equals(alsoNullColour));
            Assert.False(withNullColour.Equals(withColour));
        }
    }
}

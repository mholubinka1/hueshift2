using HueShift2.Configuration;
using NUnit.Framework;
using System;

namespace HueShift2.UnitTests.Configuration
{
    [TestFixture]
    public class GeolocatorTests
    {
        [SetUp]
        public void Setup()
        {
            //create config object
        }

        [Test]
        public void Constructor_IConfigurationSectionIsNull_ThrowsNullArgumentException()
        {
            //Arrange, Act & Assert
            Assert.Throws<ArgumentNullException>(() => new Geolocator(null));
        }

        [Test]
        public void Get_ReturnsGeolocation()
        {

        }
    }
}

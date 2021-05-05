using HueShift2.Configuration;
using Microsoft.Extensions.Configuration;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace HueShift2.UnitTests.Configuration
{
    public class GeolocatorTests
    {
        [Test]
        public void Constructor_IConfigurationSectionIsNull_ThrowsNullArgumentException()
        {
            //Arrange, Act & Assert
            Assert.Throws<ArgumentNullException>(() => new Geolocator(null));
        }


    }
}

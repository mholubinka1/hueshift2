using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HueShift2.UnitTests
{   
    [TestFixture]
    public class BuildPipelineTests
    {
        [SetUp]
        public void Setup()
        {

        }

        /*[Test]
        public void BuildPipeline_TestAlwaysFails()
        {
            var @false = false;
            Assert.That(@false == true);
        }*/

        [Test]
        public void BuildPipeline_TestAlwaysPasses()
        {
            var @true = true;
            Assert.That(@true == true);  
        }
    }
}

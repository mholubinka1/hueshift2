using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace HueShift2.Interfaces
{
    public interface IHueClientManager
    {
        public Task AssertConnected();
    }
}

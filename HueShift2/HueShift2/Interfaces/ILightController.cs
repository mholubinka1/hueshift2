using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HueShift2.Interfaces
{
    public interface ILightController
    {
        public Task ExecuteTransitionCommand();

        public Task ExecuteSynchronisationCommand();
    }
}

using HueShift2.Configuration.Model;
using System.Threading.Tasks;

namespace HueShift2.Configuration
{
    public interface IGeoLocator
    {
        public Task<Geolocation> Get(); 
    }
}

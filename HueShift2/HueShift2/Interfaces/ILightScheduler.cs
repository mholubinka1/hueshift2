using System.Threading.Tasks;

namespace HueShift2.Interfaces
{
    public interface ILightScheduler
    {
        public Task RunAsync();
    }
}

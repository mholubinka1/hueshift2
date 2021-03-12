using System.Threading.Tasks;

namespace HueShift2.Interfaces
{
    public interface ILightScheduleWorker
    {
        public Task RunAsync();
    }
}

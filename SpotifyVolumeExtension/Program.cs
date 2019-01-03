using System.Windows.Forms;
using SpotifyAPI.Web;

namespace SpotifyVolumeExtension
{
    class Program
    {
        static void Main(string[] args)
        {
            SpotifyClient sc = new SpotifyClient(AuthType.Implicit);
            sc.Start();

            VolumeGuard vg = new VolumeGuard(sc);
            vg.Start();

            SpotifyVolumeController svc = new SpotifyVolumeController(sc);
            svc.Start();

            SpotifyMonitor.GetMonitorInstance(sc).Start();
        }
    }
}

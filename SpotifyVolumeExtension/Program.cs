using SpotifyAPI.Web;

namespace SpotifyVolumeExtension
{
    class Program
    {
        static void Main(string[] args)
        {
            SpotifyClient sc = new SpotifyClient(AuthType.Implicit);
            SpotifyMonitor sm = SpotifyMonitor.GetMonitorInstance(sc);
            VolumeGuard vg = new VolumeGuard();
            SpotifyVolumeController svc = new SpotifyVolumeController(sc);

            sc.Start(sm);
            vg.Start(sm);
            svc.Start(sm);
            sm.Start();
        }
    }
}

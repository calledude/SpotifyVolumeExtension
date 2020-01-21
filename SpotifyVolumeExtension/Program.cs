using System.Threading.Tasks;

namespace SpotifyVolumeExtension
{
    public static class Program
    {
        public static async Task Main()
        {
            var sc = new SpotifyClient();
            await sc.Authenticate();

            var sm = new SpotifyMonitor(sc);
            sm.Start();

            var svc = new SpotifyVolumeController(sc);
            var vg = new VolumeGuard();

            var cc = new ConsoleController();
            cc.RegisterDisposables(svc, vg, sc, sm);
            cc.Start();
        }
    }
}

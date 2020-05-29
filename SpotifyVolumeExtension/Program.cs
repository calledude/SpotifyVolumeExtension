using System.Threading.Tasks;

namespace SpotifyVolumeExtension
{
    public static class Program
    {
        public static async Task Main()
        {
            var sc = new SpotifyClient();
            await sc.Authenticate();

            var svc = new SpotifyVolumeController(sc);
            var vg = new WindowsVolumeGuard();

            var sm = new SpotifyMonitor(sc);
            await sm.Start();

            ConsoleController.RegisterDisposables(svc, vg, sc, sm);
            ConsoleController.Start();
        }
    }
}

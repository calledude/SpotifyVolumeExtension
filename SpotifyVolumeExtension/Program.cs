using System;
using System.Threading.Tasks;

namespace SpotifyVolumeExtension
{
    public static class Program
    {
        public static async Task Main()
        {
            var sc = new SpotifyClient();

            while (!await sc.Authenticate())
            {
                Console.WriteLine("Authentication failed. Trying again in 1s.");
                await Task.Delay(1000);
            }

            var svc = new SpotifyVolumeController(sc);
            var vg = new WindowsVolumeGuard();

            var sm = new SpotifyMonitor(sc);
            await sm.Start();

            ConsoleController.RegisterDisposables(svc, vg, sc, sm);
            ConsoleController.Start();
        }
    }
}

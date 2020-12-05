using System.Threading.Tasks;

namespace SpotifyVolumeExtension
{
    public static class Program
    {
        public static async Task Main()
        {
            var messageLoopTask = Task.Factory.StartNew(ConsoleController.Start, TaskCreationOptions.LongRunning);

            var sc = new SpotifyClient();
            sc.Authenticate();

            var svc = new SpotifyVolumeController(sc);
            var vg = new WindowsVolumeGuard();

            var sm = new SpotifyMonitor(sc);
            await sm.Start();

            ConsoleController.Hide();
            ConsoleController.RegisterDisposables(svc, vg, sc, sm);

            await messageLoopTask;
        }
    }
}

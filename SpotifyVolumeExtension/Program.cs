using System.Runtime.Versioning;
using System.Threading.Tasks;

[assembly: SupportedOSPlatform("windows")]

namespace SpotifyVolumeExtension
{
	public static class Program
	{
		public static async Task Main()
		{
			var messageLoopTask = Task.Factory.StartNew(ConsoleController.Start, TaskCreationOptions.LongRunning);

			var sc = new SpotifyClient();
			sc.Authenticate();

			var sm = new SpotifyMonitor(sc);

			var svc = new SpotifyVolumeController(sc, sm.StatusController);
			var vg = new WindowsVolumeGuard();
			await sm.Start();

			ConsoleController.Hide();
			ConsoleController.RegisterDisposables(svc, vg, sc, sm);

			await messageLoopTask;
		}
	}
}

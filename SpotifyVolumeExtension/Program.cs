using SpotifyVolumeExtension.Monitoring;
using SpotifyVolumeExtension.Volume;
using System.Runtime.Versioning;
using System.Threading.Tasks;

[assembly: SupportedOSPlatform("windows")]

namespace SpotifyVolumeExtension;

public static class Program
{
	public static async Task Main()
	{
		var messageLoopTask = Task.Factory.StartNew(ConsoleController.Start, TaskCreationOptions.LongRunning);

		var spotifyClient = new SpotifyClient();
		spotifyClient.Authenticate();

		var spotifyMonitor = new SpotifyMonitor(spotifyClient);

		var spotifyVolumeController = new SpotifyVolumeController(spotifyClient, spotifyMonitor.StatusController);
		var windowsVolumeGuard = new WindowsVolumeGuard();
		await spotifyMonitor.Start();

		ConsoleController.Hide();
		ConsoleController.RegisterDisposables(spotifyVolumeController, windowsVolumeGuard, spotifyClient, spotifyMonitor);

		await messageLoopTask;
	}
}

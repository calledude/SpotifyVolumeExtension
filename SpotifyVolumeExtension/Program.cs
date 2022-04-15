using H.Hooks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nito.AsyncEx;
using Serilog;
using Serilog.Events;
using SpotifyVolumeExtension.Keyboard;
using SpotifyVolumeExtension.Monitoring;
using SpotifyVolumeExtension.Spotify;
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

		var services = new ServiceCollection()
			.AddSingleton<SpotifyClient>()
			.AddSingleton<SpotifyMonitor>()
			.AddSingleton<StatusController>()
			.AddSingleton<ProcessMonitorService>()
			.AddSingleton<VolumeControllerBase, SpotifyVolumeController>()
			.AddSingleton<VolumeControllerBase, WindowsVolumeGuard>()
			.AddTransient<AsyncMonitor>()
			.AddTransient<MediaKeyListener>()
			.AddTransient<LowLevelKeyboardHook>()
			.AddLogging(x =>
			{
				const string logFormat = "[{Timestamp:HH:mm:ss} {Level:u3} {SourceContext}] {Message:lj}{NewLine}{Exception}";
				x.ClearProviders();
				var logger = new LoggerConfiguration()
						.WriteTo.File(
							"SpotifyVolumeExtension.log",
							LogEventLevel.Verbose,
							logFormat,
							rollingInterval: RollingInterval.Day,
							retainedFileCountLimit: 5)
						.WriteTo.Console(
							LogEventLevel.Verbose,
							logFormat)
						.MinimumLevel.Verbose()
						.Enrich.FromLogContext()
						.CreateLogger();

				x.AddSerilog(logger);
				x.SetMinimumLevel(LogLevel.Trace);
			});

		var serviceProvider = services.BuildServiceProvider();

		serviceProvider
			.GetRequiredService<SpotifyClient>()
			.Authenticate();

		await serviceProvider
			.GetRequiredService<SpotifyMonitor>()
			.Start();

		ConsoleController.Hide();
		ConsoleController.RegisterDisposables(serviceProvider);

		await messageLoopTask;
	}
}

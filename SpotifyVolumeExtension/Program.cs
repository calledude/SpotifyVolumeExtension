using H.Hooks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nito.AsyncEx;
using Serilog;
using Serilog.Events;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Http;
using SpotifyVolumeExtension.Keyboard;
using SpotifyVolumeExtension.Monitoring;
using SpotifyVolumeExtension.Spotify;
using SpotifyVolumeExtension.Utilities;
using SpotifyVolumeExtension.Volume;
using System.Runtime.Versioning;
using System.Threading.Tasks;

[assembly: SupportedOSPlatform("windows5.1.2600")]

namespace SpotifyVolumeExtension;

public static class Program
{
	public static async Task Main()
	{
		const string logFormat = "[{Timestamp:HH:mm:ss} {Level:u3} {SourceContext}] {Message:lj}{NewLine}{Exception}";

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

		Log.Logger = logger;

		var messageLoopTask = Task.Factory.StartNew(ConsoleController.Start, TaskCreationOptions.LongRunning);

		AuthorizationCodeTokenResponse initialToken = null!;
		using (var tokenInitializer = new TokenInitializer())
		{
			initialToken = await tokenInitializer.InitializeToken();
		}

		var services = new ServiceCollection()
			.AddSingleton(initialToken)
			.AddSingleton<TokenSwapAuthenticator>()
			.AddSingleton<SystemTextJsonSerializer>()
			.AddSingleton<SimpleRetryHandler>()
			.AddSingleton<NetHttpClient>()
			.AddSingleton(sp =>
			{
				return new SpotifyClientConfig
				(
					SpotifyUrls.APIV1,
					sp.GetRequiredService<TokenSwapAuthenticator>(),
					sp.GetRequiredService<SystemTextJsonSerializer>(),
					sp.GetRequiredService<NetHttpClient>(),
					sp.GetRequiredService<SimpleRetryHandler>(),
					null,
					null! // Not needed for our purposes
				);
			})
			.AddSingleton<SpotifyClient>()
			.AddSingleton<SpotifyApiClient>()
			.AddSingleton<SpotifyMonitor>()
			.AddSingleton<StatusController>()
			.AddSingleton<ProcessMonitorService>()
			.AddSingleton<Retry>()
			.AddSingleton<VolumeControllerBase, SpotifyVolumeController>()
			.AddSingleton<VolumeControllerBase, WindowsVolumeGuard>()
			.AddTransient<AsyncMonitor>()
			.AddTransient<MediaKeyListener>()
			.AddTransient<LowLevelKeyboardHook>()
			.AddLogging(logBuilder =>
			{
				logBuilder
					.ClearProviders()
					.AddSerilog(Log.Logger)
					.SetMinimumLevel(LogLevel.Trace);
			});

		var serviceProvider = services.BuildServiceProvider();

		await serviceProvider
			.GetRequiredService<SpotifyMonitor>()
			.Start();

		ConsoleController.Hide();
		ConsoleController.RegisterDisposables(serviceProvider);

		await messageLoopTask;
	}
}

using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace SpotifyVolumeExtension.Monitoring;

public sealed class ProcessMonitorService
{
	private const string _processName = "Spotify";
	private readonly ILogger<ProcessMonitorService> _logger;
	private Process[] _processes;

	public event EventHandler? Exited;

	public ProcessMonitorService(ILogger<ProcessMonitorService> logger)
	{
		_processes = Process.GetProcessesByName(_processName);
		_logger = logger;
	}

	public async Task WaitForProcessToStart()
	{
		while (!ProcessIsRunning())
		{
			await Task.Delay(750);
			_processes = Process.GetProcessesByName(_processName);
		}

		_processes[0].EnableRaisingEvents = true;
		_processes[0].Exited += ProcessExited;
	}

	public bool ProcessIsRunning()
		=> _processes.Any(x => !x.HasExited);

	private async void ProcessExited(object? sender, EventArgs e)
	{
		_logger.LogInformation("Process '{processName}' has exited.", _processName);

		while (ProcessIsRunning())
		{
			await Task.Delay(50);
		}

		Exited?.Invoke(sender, e);
	}
}

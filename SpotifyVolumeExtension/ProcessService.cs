using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace SpotifyVolumeExtension;

public class ProcessService
{
	private readonly string _processName;
	private Process[] _processes;

	public event EventHandler Exited;

	public ProcessService(string processName)
	{
		_processName = processName;
		_processes = Process.GetProcessesByName(_processName);
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

	private async void ProcessExited(object sender, EventArgs e)
	{
		Console.WriteLine($"Process {_processName} has exited.");

		while (ProcessIsRunning())
		{
			await Task.Delay(50);
		}

		Exited?.Invoke(sender, e);
	}
}

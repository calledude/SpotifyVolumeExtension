using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SpotifyVolumeExtension.Volume;

public abstract class VolumeController : IDisposable
{
	private static readonly List<VolumeController> _volumeControllers = new();

	protected string Name { get; }
	protected bool Running { get; private set; }
	protected int BaselineVolume { get; set; }

	protected abstract Task<int> GetBaselineVolume();
	protected abstract Task SetNewVolume();
	protected abstract void Dispose(bool disposing);

	protected VolumeController()
	{
		Name = GetType().Name;
		_volumeControllers.Add(this);
	}

	public static async Task StartAll()
		=> await Task.WhenAll(_volumeControllers.Select(x => x.Start()));

	public static void StopAll()
	{
		foreach (var vc in _volumeControllers)
		{
			vc.Stop();
		}
	}

	protected virtual async Task Start()
	{
		BaselineVolume = await GetBaselineVolume();
		Running = true;
		Console.WriteLine($"[{Name}] Started.");
	}

	protected virtual void Stop()
	{
		Running = false;
		Console.WriteLine($"[{Name}] Stopped.");
	}

	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}
}

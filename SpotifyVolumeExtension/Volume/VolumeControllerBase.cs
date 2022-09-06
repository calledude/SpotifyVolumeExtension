using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace SpotifyVolumeExtension.Volume;

public abstract class VolumeControllerBase : IDisposable
{
	private readonly ILogger _logger;

	protected bool Running { get; private set; }
	protected int Volume { get; set; }

	protected abstract Task<int> GetBaselineVolume();
	protected abstract Task SetNewVolume();
	protected abstract void Dispose(bool disposing);

	protected VolumeControllerBase(ILogger logger)
	{
		_logger = logger;
	}

	public virtual async Task Start()
	{
		Volume = await GetBaselineVolume();
		Running = true;
		_logger.LogInformation("Started.");
	}

	public virtual void Stop()
	{
		Running = false;
		_logger.LogInformation("Stopped.");
	}

	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}
}

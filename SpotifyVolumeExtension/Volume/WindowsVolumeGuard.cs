using Microsoft.Extensions.Logging;
using NAudio.CoreAudioApi;
using System.Threading.Tasks;

namespace SpotifyVolumeExtension.Volume;

public sealed class WindowsVolumeGuard : VolumeControllerBase
{
	private readonly MMDevice _audioDeviceNaudio;

	private int SystemVolume
	{
		get => (int)(_audioDeviceNaudio.AudioEndpointVolume.MasterVolumeLevelScalar * 100);
		set => _audioDeviceNaudio.AudioEndpointVolume.MasterVolumeLevelScalar = value / 100.0f;
	}

	public WindowsVolumeGuard(ILogger<WindowsVolumeGuard> logger) : base(logger)
	{
		_audioDeviceNaudio = new MMDeviceEnumerator().GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
		_audioDeviceNaudio.AudioEndpointVolume.OnVolumeNotification += OnVolumeChange;
	}

	protected override Task<int> GetBaselineVolume()
		=> Task.FromResult(SystemVolume);

	protected override Task SetNewVolume()
	{
		SystemVolume = Volume;
		return Task.CompletedTask;
	}

	private async void OnVolumeChange(AudioVolumeNotificationData data)
	{
		if (!Running || (int)data.MasterVolume == Volume)
			return;

		await SetNewVolume();
	}

	protected override void Dispose(bool disposing)
		=> _audioDeviceNaudio.Dispose();
}

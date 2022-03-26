using NAudio.CoreAudioApi;
using System.Threading.Tasks;

namespace SpotifyVolumeExtension
{
	public sealed class WindowsVolumeGuard : VolumeController
	{
		private readonly MMDevice _audioDeviceNaudio;

		private int SystemVolume
		{
			get => (int)(_audioDeviceNaudio.AudioEndpointVolume.MasterVolumeLevelScalar * 100);
			set => _audioDeviceNaudio.AudioEndpointVolume.MasterVolumeLevelScalar = value / 100.0f;
		}

		public WindowsVolumeGuard()
		{
			_audioDeviceNaudio = new MMDeviceEnumerator().GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
			_audioDeviceNaudio.AudioEndpointVolume.OnVolumeNotification += OnVolumeChange;
		}

		protected override Task<int> GetBaselineVolume()
			=> Task.FromResult(SystemVolume);

		protected override Task SetNewVolume()
		{
			SystemVolume = BaselineVolume;
			return Task.CompletedTask;
		}

		private async void OnVolumeChange(AudioVolumeNotificationData data)
		{
			if (!Running || (int)data.MasterVolume == BaselineVolume)
				return;

			await SetNewVolume();
		}

		protected override void Dispose(bool disposing)
			=> _audioDeviceNaudio.Dispose();
	}
}

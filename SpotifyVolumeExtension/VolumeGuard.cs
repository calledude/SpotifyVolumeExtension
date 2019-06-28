using AudioSwitcher.AudioApi;
using AudioSwitcher.AudioApi.CoreAudio;
using System;

namespace SpotifyVolumeExtension
{
    public sealed class VolumeGuard : VolumeController, IObserver<DeviceVolumeChangedArgs>
    {
        private readonly CoreAudioDevice _audioDevice;
        private readonly CoreAudioController _coreAudioController;

        public VolumeGuard() : base("VolumeGuard")
        {
            _coreAudioController = new CoreAudioController();
            _audioDevice = _coreAudioController.DefaultPlaybackDevice;
            _audioDevice.VolumeChanged.Subscribe(this);
        }

        protected override int GetBaselineVolume()
            => (int)_audioDevice.Volume;

        protected override async void SetNewVolume(int volume)
        {
             await _audioDevice.SetVolumeAsync(volume);
        }

        public void OnCompleted()
            => Stop();

        public void OnError(Exception error)
            => Console.WriteLine(error.StackTrace);

        public void OnNext(DeviceVolumeChangedArgs value)
        {
            lock (_lock)
            {
                if (!Running || (int)value.Volume == BaselineVolume) return;
                SetNewVolume(BaselineVolume);
            }
        }

        protected override void Dispose(bool disposing)
        {
            _coreAudioController.Dispose();
        }
    }
}

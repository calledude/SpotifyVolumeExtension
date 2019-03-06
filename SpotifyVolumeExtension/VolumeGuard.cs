using AudioSwitcher.AudioApi;
using AudioSwitcher.AudioApi.CoreAudio;
using System;

namespace SpotifyVolumeExtension
{
    public sealed class VolumeGuard : VolumeController, IObserver<DeviceVolumeChangedArgs>
    {
        private readonly CoreAudioDevice audioDevice;

        public VolumeGuard() : base("VolumeGuard")
        {
            audioDevice = new CoreAudioController().DefaultPlaybackDevice;
            audioDevice.VolumeChanged.Subscribe(this);
        }

        protected override int GetBaselineVolume()
            => (int)audioDevice.Volume;

        protected override void SetNewVolume(int volume)
            => audioDevice.Volume = volume;

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
            audioDevice.Dispose();
        }
    }
}

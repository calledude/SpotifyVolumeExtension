using AudioSwitcher.AudioApi;
using AudioSwitcher.AudioApi.CoreAudio;
using System;
using System.Threading.Tasks;

namespace SpotifyVolumeExtension
{
    public sealed class VolumeGuard : VolumeController, IObserver<DeviceVolumeChangedArgs>
    {
        private readonly CoreAudioDevice audioDevice;
        private readonly CoreAudioController coreAudioController;

        public VolumeGuard() : base("VolumeGuard")
        {
            coreAudioController = new CoreAudioController();
            audioDevice = coreAudioController.DefaultPlaybackDevice;
            audioDevice.VolumeChanged.Subscribe(this);
        }

        protected override int GetBaselineVolume()
            => (int)audioDevice.Volume;

        protected override async void SetNewVolume(int volume)
        {
             await audioDevice.SetVolumeAsync(volume);
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
            coreAudioController.Dispose();
        }
    }
}

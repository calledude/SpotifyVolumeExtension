using AudioSwitcher.AudioApi;
using AudioSwitcher.AudioApi.CoreAudio;
using System;
using System.Threading.Tasks;

namespace SpotifyVolumeExtension
{
    public sealed class WindowsVolumeGuard : VolumeController, IObserver<DeviceVolumeChangedArgs>
    {
        private readonly CoreAudioDevice _audioDevice;
        private readonly CoreAudioController _coreAudioController;

        public WindowsVolumeGuard()
        {
            _coreAudioController = new CoreAudioController();
            _audioDevice = _coreAudioController.DefaultPlaybackDevice;
            _audioDevice.VolumeChanged.Subscribe(this);
        }

        protected override async Task<int> GetBaselineVolume()
            => (int)await _audioDevice.GetVolumeAsync();

        protected override async Task SetNewVolume()
        {
            await _audioDevice.SetVolumeAsync(BaselineVolume);
        }

        public void OnCompleted()
            => Stop();

        public void OnError(Exception error)
            => Console.WriteLine(error.ToString());

        public async void OnNext(DeviceVolumeChangedArgs value)
        {
            using (_ = await _lock.EnterAsync())
            {
                if (!Running || (int)value.Volume == BaselineVolume)
                    return;

                await SetNewVolume();
            }
        }

        protected override void Dispose(bool disposing)
        {
            _coreAudioController.Dispose();
        }
    }
}

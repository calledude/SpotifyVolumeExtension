using AudioSwitcher.AudioApi;
using AudioSwitcher.AudioApi.CoreAudio;
using System;

namespace SpotifyVolumeExtension
{
    public class VolumeGuard : IObserver<DeviceVolumeChangedArgs>
    {
        int originalVolume = 0;
        SpotifyClient sc;

        public VolumeGuard(SpotifyClient spotify)
        {
            sc = spotify;
        }

        public void Start()
        {
            CoreAudioDevice audioDevice = new CoreAudioController().DefaultPlaybackDevice;
            originalVolume = (int)audioDevice.Volume;

            var volumeChangedObserver = audioDevice.VolumeChanged;
            volumeChangedObserver.Subscribe(this);

            Console.WriteLine("[VolumeGuard] Started.");
        }

        public void OnCompleted()
        {
            Console.WriteLine("[VolumeGuard] Stopped.");
        }

        public void OnError(Exception error)
        {
            Console.WriteLine(error.Message);
        }

        public void OnNext(DeviceVolumeChangedArgs value)
        {
            if (value.Device.Volume == originalVolume) return;
            if(value.Volume < originalVolume)
            {
                sc.Volume -= 1;
            }
            else if(value.Volume > originalVolume)
            {
                sc.Volume += 1;
            }

            value.Device.Volume = originalVolume;
        }
    }
}

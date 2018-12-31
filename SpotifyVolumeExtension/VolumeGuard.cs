using AudioSwitcher.AudioApi;
using AudioSwitcher.AudioApi.CoreAudio;
using System;

namespace SpotifyVolumeExtension
{
    public class VolumeGuard : IObserver<DeviceVolumeChangedArgs>
    {
        int originalVolume = 0;
        object m = new object();
        public void Start()
        {
            CoreAudioDevice audioDevice = new CoreAudioController().DefaultPlaybackDevice;
            originalVolume = (int)audioDevice.Volume;
            
            audioDevice.VolumeChanged.Subscribe(this);
            Console.WriteLine("[VolumeGuard] Started.");
        }

        public void OnCompleted()
        {
            Console.WriteLine("[VolumeGuard] Stopped.");
        }

        public void OnError(Exception error)
        {
            Console.WriteLine(error.StackTrace);
        }
        
        public void OnNext(DeviceVolumeChangedArgs value)
        {
            lock (m)
            {
                value.Device.Volume = originalVolume;
            }
        }
    }
}

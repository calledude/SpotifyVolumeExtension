using AudioSwitcher.AudioApi;
using AudioSwitcher.AudioApi.CoreAudio;
using System;

namespace SpotifyVolumeExtension
{
    public class VolumeGuard : IObserver<DeviceVolumeChangedArgs>, IObserver<SpotifyStatusChanged>
    {
        private int originalVolume = 0;
        private object m = new object();
        CoreAudioDevice audioDevice;
        IDisposable subscriber;
        

        public void Start(SpotifyMonitor sm)
        {
            sm.Subscribe(this);
        }

        private void SetVolumeBaseline()
        {
            audioDevice = new CoreAudioController().DefaultPlaybackDevice;
            originalVolume = (int)audioDevice.Volume;
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

        public void OnNext(SpotifyStatusChanged value)
        {
            if (value.Status && subscriber == null)
            {
                SetVolumeBaseline();
                subscriber = audioDevice.VolumeChanged.Subscribe(this);
            }
            else
            {
                subscriber.Dispose();
                subscriber = null;
            }
            Console.WriteLine("[VolumeGuard] " + (value.Status ? "Started." : "Stopped."));
        }
    }
}

using AudioSwitcher.AudioApi;
using AudioSwitcher.AudioApi.CoreAudio;
using System;

namespace SpotifyVolumeExtension
{
    public class VolumeGuard : IObserver<DeviceVolumeChangedArgs>
    {
        private int originalVolume = 0;
        private object m = new object();
        CoreAudioDevice audioDevice;
        IDisposable subscriber;

        public void Start(SpotifyMonitor sm)
        {
            sm.SpotifyStatusChanged += ToggleVolumeController;
        }

        private void ToggleVolumeController(bool status)
        {
            if (status && subscriber == null)
            {
                SetVolumeBaseline();
                subscriber = audioDevice.VolumeChanged.Subscribe(this);
            }
            else
            {
                subscriber?.Dispose();
                subscriber = null;
            }
            Console.WriteLine("[VolumeGuard] " + (status ? "Started." : "Stopped."));
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
    }
}

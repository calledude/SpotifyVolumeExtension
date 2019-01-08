using AudioSwitcher.AudioApi;
using AudioSwitcher.AudioApi.CoreAudio;
using System;

namespace SpotifyVolumeExtension
{
    public class VolumeGuard : IObserver<DeviceVolumeChangedArgs>
    {
        private int originalVolume;
        private object m = new object();
        private CoreAudioDevice audioDevice;
        private bool Running;

        public VolumeGuard()
        {
            audioDevice = new CoreAudioController().DefaultPlaybackDevice;
        }

        public void Start(SpotifyMonitor sm)
        {
            sm.SpotifyStatusChanged += ToggleVolumeController;
            audioDevice.VolumeChanged.Subscribe(this);
        }

        private void ToggleVolumeController(bool status)
        {
            if (status != Running)
            {
                if (status)
                {
                    SetVolumeBaseline();
                }
                Running = status;
                Console.WriteLine("[VolumeGuard] " + (Running ? "Started." : "Stopped."));
            }
        }

        private void SetVolumeBaseline()
        {
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
                if (!Running) return;
                value.Device.Volume = originalVolume;
            }
        }
    }
}

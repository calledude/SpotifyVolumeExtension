using LowLevelInput.Hooks;
using System;
using System.Threading;

namespace SpotifyVolumeExtension
{
    public sealed class SpotifyVolumeController : VolumeController
    {
        private readonly MediaKeyListener _mkl;
        private readonly SpotifyClient _sc;
        private int _lastVolume;

        public SpotifyVolumeController(SpotifyClient sc)
        {
            this._sc = sc;
            _mkl = new MediaKeyListener();

            var debounceConfig = (TimeSpan.FromMilliseconds(50), TimeSpan.FromMilliseconds(250));
            _mkl.SubscribeTo(VirtualKeyCode.VolumeUp, debounceConfig);
            _mkl.SubscribeTo(VirtualKeyCode.VolumeDown, debounceConfig);
        }

        protected override void Start()
        {
            base.Start();

            _lastVolume = BaselineVolume;
            _mkl.SubscribedKeyPressed += VolumeKeyPressed;
        }

        protected override void Stop()
        {
            _mkl.SubscribedKeyPressed -= VolumeKeyPressed;
            base.Stop();
        }

        //Spotify web api might not be fast enough to realize we have begun playing music
        //therefore we wait for it to catch up. This happens when we press the play-key just as spotify is starting.
        protected override int GetBaselineVolume()
        {
            var playbackContext = _sc.Api.GetPlayback();
            while (playbackContext.Device == null)
            {
                Thread.Sleep(500);
                playbackContext = _sc.Api.GetPlayback();
            }
            return playbackContext.Device.VolumePercent;
        }

        private void VolumeKeyPressed(MediaKeyEventArgs m)
        {
            lock (_lock)
            {
                if (m.Key == VirtualKeyCode.VolumeUp) BaselineVolume += m.Presses;
                else BaselineVolume -= m.Presses;

                if (BaselineVolume > 100) BaselineVolume = 100;
                else if (BaselineVolume < 0) BaselineVolume = 0;

                SetNewVolume(BaselineVolume);
            }
        }

        protected override void SetNewVolume(int volume)
        {
            if (_lastVolume == volume)
                return;

            var err = _sc.Api.SetVolume(volume);
            if (err.Error == null)
            {
                Console.WriteLine($"[{Name}] Changed volume to {volume.ToString()}%");
                _lastVolume = volume;
            }
            else
            {
                BaselineVolume = _lastVolume;
            }
        }

        protected override void Dispose(bool disposing)
        {
            _mkl.Dispose();
        }
    }
}

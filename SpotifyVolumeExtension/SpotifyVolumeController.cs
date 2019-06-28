using System;
using System.Threading;
using System.Threading.Tasks;
using LowLevelInput.Hooks;

namespace SpotifyVolumeExtension
{
    public sealed class SpotifyVolumeController : VolumeController, IDisposable
    {
        private readonly MediaKeyListener _mkl;
        private readonly SpotifyClient _sc;
        private DateTime _lastVolumeChange;
        private int _lastVolume;

        public SpotifyVolumeController(SpotifyClient sc)
            : base("SpotifyVolumeController")
        {
            this._sc = sc;
            _mkl = new MediaKeyListener();
            _mkl.SubscribeTo(VirtualKeyCode.VolumeUp);
            _mkl.SubscribeTo(VirtualKeyCode.VolumeDown);
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

        private void UpdateVolume()
        {
            if (DateTime.Now - _lastVolumeChange < TimeSpan.FromMilliseconds(50))
            {
                Thread.Sleep(250);
            }

            lock (_lock)
            {
                if (_lastVolume != BaselineVolume)
                {
                    SetNewVolume(BaselineVolume);
                    _lastVolumeChange = DateTime.Now;
                    _lastVolume = BaselineVolume;
                }
            }
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
            }
            UpdateVolume();
        }

        protected override void SetNewVolume(int volume)
        {
            var err = _sc.Api.SetVolume(volume);
            if(err.Error == null)
            {
                Console.WriteLine($"[{Name}] Changed volume to {volume.ToString()}%");
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

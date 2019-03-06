using System;
using System.Threading;
using System.Threading.Tasks;
using LowLevelInput.Hooks;

namespace SpotifyVolumeExtension
{
    public sealed class SpotifyVolumeController : VolumeController, IDisposable
    {
        private readonly MediaKeyListener mkl;
        private readonly SpotifyClient sc;
        private DateTime lastVolumeChange;
        private int lastVolume;

        public SpotifyVolumeController(SpotifyClient sc)
            : base("SpotifyVolumeController")
        {
            this.sc = sc;
            mkl = new MediaKeyListener();
            mkl.SubscribeTo(VirtualKeyCode.VolumeUp);
            mkl.SubscribeTo(VirtualKeyCode.VolumeDown);
        }

        protected override void Start()
        {
            base.Start();

            lastVolume = BaselineVolume;
            mkl.SubscribedKeyPressed += VolumeKeyPressed;
        }

        protected override void Stop()
        {
            mkl.SubscribedKeyPressed -= VolumeKeyPressed;
            base.Stop();
        }

        private void UpdateVolume()
        {
            Task.Run(async () =>
            {
                if (DateTime.Now - lastVolumeChange < TimeSpan.FromMilliseconds(50))
                {
                    await Task.Delay(250);
                }

                lock (_lock)
                {
                    if (lastVolume != BaselineVolume)
                    {
                        SetNewVolume(BaselineVolume);
                        lastVolumeChange = DateTime.Now;
                        lastVolume = BaselineVolume;
                    }
                }
            });
        }

        //Spotify web api might not be fast enough to realize we have begun playing music
        //therefore we wait for it to catch up. This happens when we press the play-key just as spotify is starting.
        protected override int GetBaselineVolume()
        {
            var playbackContext = sc.Api.GetPlayback();
            while (playbackContext.Device == null)
            {
                Thread.Sleep(500);
                playbackContext = sc.Api.GetPlayback();
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
            var err = sc.Api.SetVolume(volume);
            if(err.Error == null)
            {
                Console.WriteLine($"[{Name}] Changed volume to {volume.ToString()}%");
            }
            else
            {
                BaselineVolume = lastVolume;
            }
        }

        protected override void Dispose(bool disposing)
        {
            mkl.Dispose();
        }
    }
}

using System;
using System.Threading;
using System.Windows.Forms;
using Open.WinKeyboardHook;

namespace SpotifyVolumeExtension
{
    public class MediaKeyListener
    {
        public event Action<MediaKeyEventArgs> MediaKeyPressed;
        public event Action PlayPausePressed;
        public event Action StopPressed;
        private KeyboardInterceptor key;
        private int presses = 0;

        public MediaKeyListener()
        {
            key = new KeyboardInterceptor();
            key.KeyDown += key_KeyDown;
            key.KeyUp += key_KeyUp;
        }

        public void Start()
        {
            new Thread(() =>
            {
                key.StartCapturing();
                Application.Run();
            }).Start();
        }

        private void key_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.VolumeUp || e.KeyCode == Keys.VolumeDown)
            {
                var eventArgs = new MediaKeyEventArgs()
                {
                    Presses = presses,
                    IsVolumeUp = e.KeyCode == Keys.VolumeUp
                };
                MediaKeyPressed?.Invoke(eventArgs);
                presses = 0;
            }
            else if (e.KeyCode == Keys.MediaPlayPause) PlayPausePressed?.Invoke();
            else if (e.KeyCode == Keys.MediaStop) StopPressed?.Invoke();
        }

        private void key_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.VolumeUp || e.KeyCode == Keys.VolumeDown)
            {
                presses++;
            }
        }
    }
}

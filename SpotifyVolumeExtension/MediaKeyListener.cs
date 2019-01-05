using System;
using System.Threading;
using System.Windows.Forms;
using Open.WinKeyboardHook;

namespace SpotifyVolumeExtension
{
    public class MediaKeyListener
    {
        public event Action<MediaKeyEventArgs> MediaKeyPressed;
        public KeyboardInterceptor key;
        private int presses = 0;
        private bool runHook;

        public MediaKeyListener()
        {
            key = new KeyboardInterceptor();
            key.KeyDown += key_KeyDown;
            key.KeyUp += key_KeyUp;
        }

        public void Start()
        {
            runHook = true;
            new Thread(() =>
            {
                key.StartCapturing();
                while (runHook)
                {
                    Thread.Sleep(1);
                    Application.DoEvents();
                }
                key.StopCapturing();
            }).Start();

            Console.WriteLine("[MediaKeyListener] Started.");
        }

        public void Stop()
        {
            runHook = false;
            Console.WriteLine("[MediaKeyListener] Stopped.");
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

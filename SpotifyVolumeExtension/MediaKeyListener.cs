using System;
using System.Threading;
using System.Windows.Forms;
using Open.WinKeyboardHook;

namespace SpotifyVolumeExtension
{
    public class MediaKeyListener
    {
        public event Action<MediaKeyEventArgs> MediaKeyPressed;
        private MediaKeyEventArgs eventArgs;
        public KeyboardInterceptor key;
        private int presses = 0;
        private Thread mklThread;

        public MediaKeyListener()
        {
            key = new KeyboardInterceptor();
            key.KeyDown += key_KeyDown;
            key.KeyUp += key_KeyUp;
        }

        public void Start()
        {
            mklThread = new Thread(() =>
            {
                key.StartCapturing();
                Console.WriteLine("[MediaKeyListener] Started.");
                Application.Run();
            });
            mklThread.Start();
        }

        public void Stop()
        {
            mklThread.Abort();
            key.StopCapturing();
            Console.WriteLine("[MediaKeyListener] Stopped.");
        }

        private void key_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.VolumeUp || e.KeyCode == Keys.VolumeDown)
            {
                eventArgs = new MediaKeyEventArgs()
                {
                    Presses = presses,
                    Key = e.KeyCode == Keys.VolumeDown 
                                        ? KeyType.Down 
                                        : KeyType.Up
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

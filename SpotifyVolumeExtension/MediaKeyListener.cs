using System;
using System.Windows.Forms;
using Open.WinKeyboardHook;

namespace SpotifyVolumeExtension
{
    public class MediaKeyListener
    {
        private event Action<MediaKeyEventArgs> MediaKeyPressed;
        private MediaKeyEventArgs eventArgs;
        private KeyboardInterceptor key;
        private int presses = 0;

        public MediaKeyListener(Action<MediaKeyEventArgs> mediaKeyPressed)
        {
            key = new KeyboardInterceptor();
            key.KeyDown += key_KeyDown;
            key.KeyUp += key_KeyUp;
            MediaKeyPressed = mediaKeyPressed;
        }

        public void Start()
        {
            key.StartCapturing();
            Console.WriteLine("[MediaKeyListener] Started.");
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

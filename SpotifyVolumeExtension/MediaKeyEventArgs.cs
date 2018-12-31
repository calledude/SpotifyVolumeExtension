using System;

namespace SpotifyVolumeExtension
{
    public enum KeyType
    {
        Up, Down
    }

    public class MediaKeyEventArgs : EventArgs
    {
        public MediaKeyEventArgs()
        {
            When = DateTime.Now;
        }
        public KeyType Key { get; set; }
        public int Presses { get; set; }
        public DateTime When { get; }
    }
}

using System;

namespace SpotifyVolumeExtension
{
    public enum KeyType
    {
        Up, Down
    }
    public class MediaKeyEventArgs : EventArgs
    {
        public KeyType Key { get; set; }
        public int Presses { get; set; }
    }
}

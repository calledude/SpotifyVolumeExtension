using H.Hooks;
using System;

namespace SpotifyVolumeExtension
{
	public sealed class MediaKeyEventArgs : EventArgs
	{
		public MediaKeyEventArgs()
		{
			When = DateTime.Now;
		}

		public Key Key { get; set; }
		public int Presses { get; set; }
		public DateTime When { get; }
	}
}

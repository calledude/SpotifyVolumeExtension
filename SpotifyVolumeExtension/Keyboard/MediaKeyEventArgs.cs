using H.Hooks;
using System;

namespace SpotifyVolumeExtension.Keyboard;

public sealed class MediaKeyEventArgs : EventArgs
{
	public MediaKeyEventArgs()
	{
		When = DateTime.UtcNow;
	}

	public Key Key { get; set; }
	public int Presses { get; set; }
	public DateTime When { get; }
}

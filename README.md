# SpotifyVolumeExtension

### THIS BRANCH CONTAINS CODE CHANGES THAT WILL ONLY WORK PROPERLY ONCE **[THIS](https://github.com/spotify/web-api/issues/1133)** IS RESOLVED.


Makes Windows respect volume-button input when Spotify is running.

Since Windows refuses to recognize when other programs that use the media buttons might be running, I decided to fix that.
This is a simple program that will allow you to change your Spotify-volume when it is running, with your media keys.

Let's rise up against Windows and take back control of our media keys! *Viva La Résistance*!

### Disclaimer:
This repo includes the SpotifyAPI-NET Library with a few changes/tweaks. My fork of it can be found [here](https://github.com/calledude/SpotifyAPI-NET). The .dll is however included for your convenience.

The program is fully functional as is, it includes my own App Client-ID. However, this comes with a small caveat. Without a Client-Secret, it has to go through the authentication process once the token expires (every hour or so). This will open the web-browser once again, which can be a bit annoying. If you wish to remove this behaviour you will have to create your own app at the [Spotify Developer Dashboard](https://developer.spotify.com/) and set the Client-ID and Client-Secret in the source-code accordingly.

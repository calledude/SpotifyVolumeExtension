namespace SpotifyVolumeExtension
{
    public enum AuthType
    {
        Authorization, Implicit
    }


    class Program
    {
        static void Main(string[] args)
        {
            ConsoleController t = new ConsoleController();

            MediaKeyListener mkl = new MediaKeyListener();
            mkl.Start();

            SpotifyClient sc = new SpotifyClient(AuthType.Implicit);
            sc.Start();

            SpotifyMonitor sm = new SpotifyMonitor(sc, mkl);
            sm.Start();

            t.Start();
        }
    }
}

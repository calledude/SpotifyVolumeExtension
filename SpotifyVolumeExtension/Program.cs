using SpotifyAPI.Web.Auth;

namespace SpotifyVolumeExtension
{
    public static class Program
    {
        public static void Main()
        {
            var sc = new SpotifyClient<ImplicitGrantAuth>();
            sc.Authenticate();

            var sm = new SpotifyMonitor(sc);
            sm.Start();

            var cc = new ConsoleController();
            cc.Start();
        }
    }
}

using System;
using System.Linq;
using System.Diagnostics;
using System.Threading;
using System.Collections.Concurrent;

namespace SpotifyVolumeExtension
{
    public class SpotifyMonitor : IObservable<SpotifyStatusChanged>
    {
        private SpotifyClient sc;
        private bool lastSpotifyStatus;
        private bool started;
        private ConcurrentBag<IObserver<SpotifyStatusChanged>> observers;
        private static object m = new object();

        private static SpotifyMonitor sm;

        SpotifyMonitor(SpotifyClient sc)
        {
            this.sc = sc;
            observers = new ConcurrentBag<IObserver<SpotifyStatusChanged>>();
        }

        public static SpotifyMonitor GetMonitorInstance(SpotifyClient sc)
        {
            lock (m)
            {
                if (sm == null) sm = new SpotifyMonitor(sc);
                return sm;
            }
        }

        public void Start()
        {
            if (started) return;
            Console.WriteLine("[SpotifyMonitor] Started. Now monitoring activity.");
            started = true;
            new Thread(() =>
            {
                while (true)
                {
                    if (GetPlayingStatus() != lastSpotifyStatus)
                    {
                        lastSpotifyStatus = !lastSpotifyStatus;
                        foreach(var observer in observers)
                        {
                            observer.OnNext(new SpotifyStatusChanged(lastSpotifyStatus));
                        }
                    }
                    Thread.Sleep(15000);
                }
            }).Start();
        }

        public bool GetPlayingStatus()
        {
            return SpotifyIsRunning() && IsPlayingMusic();
        }

        private bool SpotifyIsRunning()
        {
            var spotifyProcesses = Process.GetProcessesByName("Spotify");
            return spotifyProcesses.Any();
        }

        private bool IsPlayingMusic()
        {
            return sc.AnyDeviceIsActive;
        }

        //IObservable<SpotifyStatusChanged> stuff below
        public IDisposable Subscribe(IObserver<SpotifyStatusChanged> observer)
        {
            if (!observers.Contains(observer))
                observers.Add(observer);
            return new Unsubscriber(observers, observer);
        }

        private class Unsubscriber : IDisposable
        {
            private ConcurrentBag<IObserver<SpotifyStatusChanged>> _observers;
            private IObserver<SpotifyStatusChanged> _observer;

            public Unsubscriber(ConcurrentBag<IObserver<SpotifyStatusChanged>> observers, IObserver<SpotifyStatusChanged> observer)
            {
                this._observers = observers;
                this._observer = observer;
            }

            public void Dispose()
            {
                if (_observer != null && _observers.Contains(_observer))
                {
                    _observer.OnCompleted();
                    _observers.TryTake(out _observer);
                }
            }
        }
    }

    public struct SpotifyStatusChanged
    {
        public SpotifyStatusChanged(bool status)
        {
            Status = status;
        }
        public bool Status { get; }
    }

}

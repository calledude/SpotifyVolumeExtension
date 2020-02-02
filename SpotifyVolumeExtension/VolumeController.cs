using System;
using System.Collections.Generic;

namespace SpotifyVolumeExtension
{
    public abstract class VolumeController : IDisposable
    {
        private static readonly List<VolumeController> _volumeControllers = new List<VolumeController>();

        protected string Name { get; }
        protected bool Running { get; private set; }
        protected int BaselineVolume { get; set; }
        protected object _lock { get; }

        protected abstract int GetBaselineVolume();
        protected abstract void SetNewVolume(int volume);
        protected abstract void Dispose(bool disposing);

        protected VolumeController()
        {
            Name = GetType().Name;
            _volumeControllers.Add(this);
            _lock = new object();
        }

        public static void StartAll()
        {
            foreach (var vc in _volumeControllers)
            {
                vc.Start();
            }
        }

        public static void StopAll()
        {
            foreach (var vc in _volumeControllers)
            {
                vc.Stop();
            }
        }

        protected virtual void Start()
        {
            BaselineVolume = GetBaselineVolume();
            Running = true;
            Console.WriteLine($"[{Name}] Started.");
        }

        protected virtual void Stop()
        {
            Running = false;
            Console.WriteLine($"[{Name}] Stopped.");
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }
}

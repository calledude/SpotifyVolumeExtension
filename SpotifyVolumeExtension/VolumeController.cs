﻿using Nito.AsyncEx;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SpotifyVolumeExtension
{
    public abstract class VolumeController : IDisposable
    {
        private static readonly List<VolumeController> _volumeControllers = new List<VolumeController>();

        protected string Name { get; }
        protected bool Running { get; private set; }
        protected int BaselineVolume { get; set; }
        protected AsyncMonitor _lock { get; }

        protected abstract Task<int> GetBaselineVolume();
        protected abstract Task SetNewVolume();
        protected abstract void Dispose(bool disposing);

        protected VolumeController()
        {
            Name = GetType().Name;
            _volumeControllers.Add(this);
            _lock = new AsyncMonitor();
        }

        public static async Task StartAll()
        {
            foreach (var vc in _volumeControllers)
            {
                await vc.Start();
            }
        }

        public static void StopAll()
        {
            foreach (var vc in _volumeControllers)
            {
                vc.Stop();
            }
        }

        protected virtual async Task Start()
        {
            BaselineVolume = await GetBaselineVolume();
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

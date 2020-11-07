﻿using System;
using System.Threading.Tasks;

namespace SpotifyVolumeExtension
{
    public static class Retry
    {
        private const int _maxRetries = 5;

        public static async Task<T> Wrap<T>(Func<Task<T>> wrapSubject)
        {
            var retries = 0;
            while (true)
            {
                try
                {
                    retries++;
                    return await wrapSubject();
                }
                catch (Exception ex)
                {
                    if (retries >= _maxRetries)
                    {
                        Console.WriteLine("Max retries exceeded. Bailing.");
                        return default;
                    }

                    Console.WriteLine($"Retrying - {ex.GetType().Name} thrown.");
                    await Task.Delay(TimeSpan.FromMilliseconds(500));
                }
            }
        }
    }
}

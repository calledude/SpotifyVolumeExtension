using System;
using System.Threading.Tasks;

namespace SpotifyVolumeExtension;

public static class Retry
{
	private const int MAXRETRIES = 5;

	public static async Task<T> Wrap<T>(Func<Task<T>> wrapSubject)
		=> await WrapInternal(wrapSubject);

	public static async Task Wrap(Func<Task> wrapSubject)
		=> await WrapInternal(async () =>
		{
			await wrapSubject();
			return 1;
		});

	private static async Task<T> WrapInternal<T>(Func<Task<T>> wrapSubject)
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
				if (retries >= MAXRETRIES)
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

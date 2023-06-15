using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using System;
using System.Threading.Tasks;

namespace SpotifyVolumeExtension.Utilities;

public class Retry
{
	private const int _maxRetries = 5;
	private readonly ILogger<Retry> _logger;
	private readonly AsyncRetryPolicy _retryPolicy;

	public Retry(ILogger<Retry> logger)
	{
		_logger = logger;

		_retryPolicy = Policy.Handle<Exception>()
			.WaitAndRetryAsync(
				_maxRetries,
				(retry) => retry * TimeSpan.FromMilliseconds(500),
				(ex, _) => _logger.LogWarning("Retrying - {exceptionType} thrown.", ex.GetType().Name));
	}

	public async Task<T> Wrap<T>(Func<Task<T>> retrySubject)
	{
		var result = await _retryPolicy.ExecuteAndCaptureAsync(retrySubject);
		HandleOutcome(result.Outcome);
		return result.Result;
	}

	public async Task Wrap(Func<Task> retrySubject)
	{
		var result = await _retryPolicy.ExecuteAndCaptureAsync(retrySubject);
		HandleOutcome(result.Outcome);
	}

	private void HandleOutcome(OutcomeType outcomeType)
	{
		if (outcomeType != OutcomeType.Failure)
			return;

		_logger.LogWarning("Max retries exceeded. Bailing.");
	}
}

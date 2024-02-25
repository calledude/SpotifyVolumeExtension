using SpotifyAPI.Web.Http;
using SpotifyVolumeExtension.Spotify.Converters;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SpotifyVolumeExtension.Spotify;

public class SystemTextJsonSerializer : IJSONSerializer
{
	private readonly JsonSerializerOptions _options = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
		Converters =
		{
			new PlayableItemConverter(),
			new JsonStringEnumConverter()
		}
	};

	public IAPIResponse<T> DeserializeResponse<T>(IResponse response)
	{
		if (response.ContentType is not "application/json" || response.Body is not string body)
			return new APIResponse<T>(response);

		var data = JsonSerializer.Deserialize<T>(body, _options);
		return new APIResponse<T>(response, data);
	}

	public void SerializeRequest(IRequest request)
	{
		if (request.Body is string or Stream or HttpContent or null)
			return;

		request.Body = JsonSerializer.Serialize(request.Body, _options);
	}
}

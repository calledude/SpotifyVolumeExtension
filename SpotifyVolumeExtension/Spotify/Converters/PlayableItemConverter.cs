using SpotifyAPI.Web;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SpotifyVolumeExtension.Spotify.Converters;

public class PlayableItemConverter : JsonConverter<IPlayableItem>
{
	public override IPlayableItem? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		var document = JsonDocument.ParseValue(ref reader);
		var type = document.RootElement.GetProperty("type"u8);

		return type.GetString() switch
		{
			"track" => document.Deserialize<FullTrack>(options),
			"episode" => document.Deserialize<FullEpisode>(options),
			_ => throw new ArgumentException("Unknown type")
		};
	}

	public override void Write(Utf8JsonWriter writer, IPlayableItem value, JsonSerializerOptions options) => throw new NotImplementedException();
}
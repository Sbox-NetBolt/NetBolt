using System.Text.Json.Serialization;

namespace NetBolt.Extensions.Sbox;

[method: JsonConstructor]
internal readonly struct SboxAuthTokenResponse( long steamId, string status )
{
	public long SteamId { get; } = steamId;
	public string Status { get; } = status;

	public bool IsComplete => SteamId != 0 && Status is not null && Status.Length != 0;
}

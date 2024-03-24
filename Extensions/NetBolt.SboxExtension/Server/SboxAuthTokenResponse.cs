using System.Text.Json.Serialization;

namespace NetBolt.Extensions.Sbox;

internal readonly struct SboxAuthTokenResponse
{
	public long SteamId { get; }
	public string Status { get; }

	public bool IsComplete => SteamId != 0 && Status is not null && Status.Length != 0;

	[JsonConstructor]
	public SboxAuthTokenResponse( long steamId, string status )
	{
		SteamId = steamId;
		Status = status;
	}
}

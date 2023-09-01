namespace NetBolt.Server.Extensions;

public static class NetBoltServerExtensions
{
	public static Client? GetClientByGenericId( this NetBoltServer server, long genericId )
	{
		return server.GetClientByIdentifier( ClientIdentifier.FromGeneric( genericId ) );
	}

	public static Client? GetClientBySteamId64( this NetBoltServer server, long steamId )
	{
		return server.GetClientByIdentifier( ClientIdentifier.FromSteamId64( steamId ) );
	}
}

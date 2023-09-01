namespace NetBolt.Extensions;

public static class RealmExtensions
{
	public static bool IsClient( this Realm realm ) => realm == Realm.Client;
	public static bool IsServer( this Realm realm ) => realm == Realm.Server;
}

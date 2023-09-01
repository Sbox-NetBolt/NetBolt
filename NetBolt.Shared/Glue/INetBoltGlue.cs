using NetBolt.Glue.Logging;

namespace NetBolt.Glue;

public interface INetBoltGlue : IReflectionHandler
{
	Realm Realm { get; }

	ILogger Logger { get; }

	IClientConnection ClientConnection { get; }
	IServerHost ServerHost { get; }

	bool StringCachingEnabled { get; }
	StringCache StringCache { get; }
}

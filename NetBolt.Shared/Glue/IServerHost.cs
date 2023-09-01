using NetBolt.Messaging;

namespace NetBolt.Glue;

public interface IServerHost
{
	void SendMessageTo( IClient client, NetworkMessage message );
}

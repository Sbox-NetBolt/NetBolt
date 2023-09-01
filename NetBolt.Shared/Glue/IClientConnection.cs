using NetBolt.Messaging;

namespace NetBolt.Glue;

public interface IClientConnection
{
	void SendMessageToServer( NetworkMessage message );
}

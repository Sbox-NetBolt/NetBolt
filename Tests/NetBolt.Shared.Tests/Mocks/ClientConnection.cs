using NetBolt.Glue;
using NetBolt.Messaging;

namespace NetBolt.Tests.Shared.Mocks;

internal sealed class ClientConnection : IClientConnection
{
	public void SendMessageToServer( NetworkMessage message )
	{
		throw new System.NotImplementedException();
	}
}

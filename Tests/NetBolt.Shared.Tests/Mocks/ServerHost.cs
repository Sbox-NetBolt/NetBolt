using NetBolt.Glue;
using NetBolt.Messaging;

namespace NetBolt.Tests.Shared.Mocks;

internal sealed class ServerHost : IServerHost
{
	public void SendMessageTo( IClient client, NetworkMessage message )
	{
		throw new System.NotImplementedException();
	}
}

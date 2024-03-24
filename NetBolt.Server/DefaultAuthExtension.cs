using NetBolt.Server.Extensions;
using System.Threading.Tasks;
using vtortola.WebSockets;

namespace NetBolt.Server;

public sealed class DefaultAuthExtension : NetBoltServerExtension
{
	public override string Name => "Default Auth";

	private static long GenericIdentifier;

	public override ValueTask<bool> OnNegotiateSocketAsync( WebSocketHttpRequest request, WebSocketHttpResponse response )
	{
		request.StoreIdentifier( new ClientIdentifier( Platform.Generic, ++GenericIdentifier ), this );
		return ValueTask.FromResult( true );
	}
}

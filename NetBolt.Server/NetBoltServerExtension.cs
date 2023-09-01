using NetBolt.Messaging;
using NetBolt.Shared;
using System.Threading.Tasks;
using vtortola.WebSockets;
using ILogger = NetBolt.Glue.Logging.ILogger;

namespace NetBolt.Server;

public abstract class NetBoltServerExtension : IExtension
{
	public abstract string Name { get; }
	public NetBoltServer Server { get; internal set; } = null!;
	protected internal ILogger Logger { get; internal set; } = null!;

	public virtual void Start()
	{
	}

	public virtual void Stop()
	{
	}

	public virtual void ProcessEvents()
	{
	}

	public virtual void OnClientConnected( Client client )
	{
	}

	public virtual void OnClientDisconnected( Client client )
	{
	}

	public virtual bool OnClientMessageReceived( Client client, NetworkMessage message )
	{
		return false;
	}

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
	public virtual async ValueTask<bool> OnNegotiateSocketAsync( WebSocketHttpRequest request, WebSocketHttpResponse response )
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
	{
		return true;
	}
}

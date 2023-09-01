using NetBolt.Attributes;

namespace NetBolt.Messaging.Messages;

public sealed class DisconnectMessage : NetworkMessage
{
	public ServerDisconnectReason Reason { get; private set; }

	[ForReplication]
	public DisconnectMessage()
	{
	}

	public DisconnectMessage( ServerDisconnectReason reason )
	{
		Reason = reason;
	}

	public override void Serialize( NetworkMessageWriter writer )
	{
		writer.Write( (byte)Reason );
	}

	public override void Deserialize( NetworkMessageReader reader )
	{
		Reason = (ServerDisconnectReason)reader.ReadByte();
	}
}

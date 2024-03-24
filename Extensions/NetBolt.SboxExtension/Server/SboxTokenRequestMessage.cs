namespace NetBolt.Messaging.Messages;

public sealed class SboxTokenRequestMessage : NetworkMessage
{
	public string? Token { get; private set; }

	public SboxTokenRequestMessage()
	{
	}

	public SboxTokenRequestMessage( string token )
	{
		Token = token;
	}

	public override void Serialize( NetworkMessageWriter writer )
	{
		writer.Write( Token is not null );
		if ( Token is not null )
			writer.Write( Token );
	}

	public override void Deserialize( NetworkMessageReader reader )
	{
		if ( reader.ReadBoolean() )
			Token = reader.ReadString();
	}
}

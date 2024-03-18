using NetBolt.Attributes;
using NetBolt.Glue;
using System;
using System.Collections.Generic;
using System.Text;

namespace NetBolt.Messaging.Messages;

public sealed class PartialMessage : NetworkMessage
{
	public const int PartialHeaderSize = sizeof( int ) + sizeof( int );
	// NumPieces + PartialData.Count

	public override bool CacheTypeString => false;

	public int NumPieces { get; private set; }

	public ArraySegment<byte> PartialData { get; private set; }

	[ForReplication]
	public PartialMessage()
	{
	}

	public PartialMessage( int numPieces, in ArraySegment<byte> partialData )
	{
		NumPieces = numPieces;
		PartialData = partialData;
	}

	public override void Serialize( NetworkMessageWriter writer )
	{
		writer.Write( NumPieces );

		writer.Write( PartialData.Count );
		writer.Write( PartialData.AsSpan() );
	}

	public override void Deserialize( NetworkMessageReader reader )
	{
		NumPieces = reader.ReadInt32();
		PartialData = reader.ReadBytes( reader.ReadInt32() );
	}

	public static IEnumerable<PartialMessage> CreateFrom( INetBoltGlue glue, ArraySegment<byte> messageBytes, int maxMessageSize, Encoding encoding )
	{
		ArgumentNullException.ThrowIfNull( glue, nameof( glue ) );
		ArgumentNullException.ThrowIfNull( messageBytes.Array, nameof( messageBytes ) );
		ArgumentNullException.ThrowIfNull( encoding, nameof( encoding ) );

		var partialMessageHeaderSize = GetHeaderSize<PartialMessage>( glue, encoding );
		var partialDataPerMessage = maxMessageSize - partialMessageHeaderSize;
		if ( partialDataPerMessage <= 0 )
			throw new ArgumentException( $"{nameof( maxMessageSize )} is too small, it must be > {partialMessageHeaderSize}", nameof( maxMessageSize ) );

		var numMessages = (int)Math.Ceiling( (double)maxMessageSize / partialDataPerMessage );

		for ( var i = 0; i < messageBytes.Count; i += partialDataPerMessage )
		{
			var startIndex = i;
			var length = Math.Min( messageBytes.Count - startIndex, partialDataPerMessage );

			var partialBytes = messageBytes.Slice( startIndex, length ).ToArray();
			var partialMessage = new PartialMessage( numMessages, new ArraySegment<byte>( partialBytes ) );

			yield return partialMessage;
		}
	}
}

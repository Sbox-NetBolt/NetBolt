using NetBolt.Attributes;
using NetBolt.Glue;
using System;
using System.Collections.Generic;
using System.IO;
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

	public static IEnumerable<(PartialMessage PartialMessage, long PartialMessageSize)> CreateFrom( INetBoltGlue glue,
		byte[] messageBytes, long messageSize, int maxMessageSize,
		Encoding networkMessageCharacterEncoding, byte[] partialMessageBytes )
	{
		ArgumentNullException.ThrowIfNull( glue, nameof( glue ) );
		ArgumentNullException.ThrowIfNull( messageBytes, nameof( messageBytes ) );
		ArgumentNullException.ThrowIfNull( networkMessageCharacterEncoding, nameof( networkMessageCharacterEncoding ) );
		ArgumentNullException.ThrowIfNull( partialMessageBytes, nameof( partialMessageBytes ) );

		var partialMessageHeaderSize = GetHeaderSize<PartialMessage>( glue, networkMessageCharacterEncoding );
		var partialDataPerMessage = maxMessageSize - partialMessageHeaderSize;
		var numMessages = (int)Math.Ceiling( (double)messageSize / partialDataPerMessage );

		for ( var i = 0; i < messageSize; i += partialDataPerMessage )
		{
			var startIndex = i;
			var length = (int)Math.Min( messageSize - startIndex, partialDataPerMessage );

			var partialMessage = new PartialMessage( numMessages, new ArraySegment<byte>( messageBytes, startIndex, length ) );
			using var partialDataStream = new MemoryStream( partialMessageBytes, true );

			WriteToStream( glue, partialDataStream, partialMessage, networkMessageCharacterEncoding );
			var partialMessageSize = partialDataStream.Position;

			yield return (partialMessage, partialMessageSize);
		}
	}
}

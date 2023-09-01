using NetBolt.Glue;
using NetBolt.Messaging.Messages;
using System;
using System.IO;
using System.Text;

namespace NetBolt.Messaging;

public abstract class NetworkMessage
{
	public const int HeaderSize = sizeof( bool ) + sizeof( char ) * 256;

	public virtual bool CacheTypeString => true;

	public abstract void Serialize( NetworkMessageWriter writer );
	public abstract void Deserialize( NetworkMessageReader reader );

	public static int GetHeaderSize<T>( INetBoltGlue glue ) where T : NetworkMessage, new() => GetHeaderSize( glue, new T(), Encoding.Default );
	public static int GetHeaderSize<T>( INetBoltGlue glue, Encoding encoding ) where T : NetworkMessage, new() => GetHeaderSize( glue, new T(), encoding );
	public static int GetHeaderSize( INetBoltGlue glue, NetworkMessage message ) => GetHeaderSize( glue, message, Encoding.Default );
	public static int GetHeaderSize( INetBoltGlue glue, NetworkMessage message, Encoding encoding )
	{
		ArgumentNullException.ThrowIfNull( message, nameof( message ) );
		ArgumentNullException.ThrowIfNull( encoding, nameof( encoding ) );

		var size = 0;
		if ( message.CacheTypeString )
		{
			size += sizeof( bool ); // Cache bool.
			if ( !glue.StringCachingEnabled )
			{
				var messageType = message.GetType();
				size += encoding.GetByteCount( messageType.FullName ?? messageType.Name ); // Literal string encoded.
				size += 1; // Null byte for string.
			}
			else
				size += sizeof( uint ); // String cache ID.
		}
		else
		{
			var messageType = message.GetType();
			size += encoding.GetByteCount( messageType.FullName ?? messageType.Name ); // Literal string encoded.
			size += 1; // Null byte for string.
		}

		if ( message is PartialMessage )
			size += PartialMessage.PartialHeaderSize;

		return size;
	}

	public static void WriteToStream( INetBoltGlue glue, Stream stream, NetworkMessage message ) => WriteToStream( glue, stream, message, Encoding.Default );
	public static void WriteToStream( INetBoltGlue glue, Stream stream, NetworkMessage message, Encoding encoding )
	{
		ArgumentNullException.ThrowIfNull( stream, nameof( stream ) );
		ArgumentNullException.ThrowIfNull( message, nameof( message ) );
		ArgumentNullException.ThrowIfNull( encoding, nameof( encoding ) );

		if ( !stream.CanWrite )
			throw new ArgumentException( "The stream cannot be written to", nameof( stream ) );

		using var writer = new NetworkMessageWriter( stream, encoding, true, glue );

		var messageType = message.GetType();
		if ( message.CacheTypeString )
			writer.WriteCacheString( messageType.FullName ?? messageType.Name );
		else
		{
			writer.Write( false );
			writer.Write( messageType.FullName ?? messageType.Name );
		}

		message.Serialize( writer );
	}

	public static NetworkMessage Parse( INetBoltGlue glue, byte[] data ) => Parse( glue, data, Encoding.Default, out _ );
	public static NetworkMessage Parse( INetBoltGlue glue, byte[] data, out long messageSize ) => Parse( glue, data, Encoding.Default, out messageSize );
	public static NetworkMessage Parse( INetBoltGlue glue, byte[] data, Encoding encoding ) => Parse( glue, data, encoding, out _ );
	public static NetworkMessage Parse( INetBoltGlue glue, byte[] data, Encoding encoding, out long messageSize )
	{
		ArgumentNullException.ThrowIfNull( data );
		ArgumentNullException.ThrowIfNull( encoding );

		using var stream = new MemoryStream( data );
		using var reader = new NetworkMessageReader( stream, encoding, glue );

		var type = reader.ReadType();
		if ( !type.IsAssignableTo( typeof( NetworkMessage ) ) )
			throw new InvalidCastException( $"Received type is not assignable to {nameof( NetworkMessage )}" );

		var message = (NetworkMessage)glue.CreateInstance( type )!;
		message.Deserialize( reader );
		messageSize = stream.Position;
		return message;
	}
}

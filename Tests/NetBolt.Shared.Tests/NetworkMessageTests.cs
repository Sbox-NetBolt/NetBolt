using NetBolt.Messaging;
using NetBolt.Messaging.Messages;
using NetBolt.Tests.Shared.Mocks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Xunit;

namespace NetBolt.Tests.Shared;

public class NetworkMessageTests
{
	#region GetHeaderSize
	[Fact]
	public void GetHeaderSizeCached()
	{
		// Given:
		const int expectedSize = sizeof( bool ) + sizeof( uint );
		NetworkMessage.InvalidateSizeCache();

		var glue = new ServerGlue();
		glue.StringCache = new StringCache( glue );
		glue.StringCachingEnabled = true;

		// When:
		var size = NetworkMessage.GetHeaderSize<CachedTypeNetworkMessage>( glue, Encoding.Default );

		// Then:
		Assert.Equal( expectedSize, size );
	}

	[Fact]
	public void GetHeaderSizeCachedStringCacheDisabled()
	{
		// Given:
		var type = typeof( CachedTypeNetworkMessage );
		var typeName = type.FullName ?? type.Name;
		var encoding = Encoding.Default;
		var expectedSize = sizeof( bool ) + encoding.GetByteCount( typeName ) + 1;
		NetworkMessage.InvalidateSizeCache();

		// When:
		var size = NetworkMessage.GetHeaderSize<CachedTypeNetworkMessage>( new ServerGlue(), Encoding.Default );

		// Then:
		Assert.Equal( expectedSize, size );
	}

	[Fact]
	public void GetHeaderSizeUncached()
	{
		// Given:
		var type = typeof( UncachedTypeNetworkMessage );
		var typeName = type.FullName ?? type.Name;
		var encoding = Encoding.Default;
		var expectedSize = encoding.GetByteCount( typeName ) + 1;
		NetworkMessage.InvalidateSizeCache();

		// When:
		var size = NetworkMessage.GetHeaderSize<UncachedTypeNetworkMessage>( new ServerGlue(), encoding );

		// Then:
		Assert.Equal( expectedSize, size );
	}

	[Fact]
	public void GetHeaderSizePartialMessage()
	{
		// Given:
		var type = typeof( PartialMessage );
		var typeName = type.FullName ?? type.Name;
		var encoding = Encoding.Default;
		var expectedSize = encoding.GetByteCount( typeName ) + 1 + PartialMessage.PartialHeaderSize;
		NetworkMessage.InvalidateSizeCache();

		// When:
		var size = NetworkMessage.GetHeaderSize<PartialMessage>( new ServerGlue() );

		// Then:
		Assert.Equal( expectedSize, size );
	}

	[Fact]
	public void GetHeaderSizeThrowsOnNullGlue()
	{
		// Given:
		const string getHeaderSizeParameterName = "glue";

		// When:
		static void Execute()
		{
			var size = NetworkMessage.GetHeaderSize( null!, new CachedTypeNetworkMessage() );
		}

		// Then:
		Assert.Throws<ArgumentNullException>( getHeaderSizeParameterName, Execute );
	}

	[Fact]
	public void GetHeaderSizeThrowsOnNullMessage()
	{
		// Given:
		const string getHeaderSizeParameterName = "message";

		// When:
		static void Execute()
		{
			var size = NetworkMessage.GetHeaderSize( new ServerGlue(), null! );
		}

		// Then:
		Assert.Throws<ArgumentNullException>( getHeaderSizeParameterName, Execute );
	}

	[Fact]
	public void GetHeaderSizeThrowsOnNullEncoding()
	{
		// Given:
		const string getHeaderSizeParameterName = "encoding";

		// When:
		static void Execute()
		{
			var size = NetworkMessage.GetHeaderSize( new ServerGlue(), new CachedTypeNetworkMessage(), null! );
		}

		// Then:
		Assert.Throws<ArgumentNullException>( getHeaderSizeParameterName, Execute );
	}
	#endregion

	#region WriteToStream
	[Fact]
	public void WriteToStreamCached()
	{
		// Given:
		var messageType = typeof( CachedTypeNetworkMessage );
		var messageTypeName = messageType.FullName ?? messageType.Name;
		const uint cacheId = 1;
		const float property1 = 123;
		const string property2 = "Hello, World!";

		var glue = new ClientGlue();
		glue.StringCache = new StringCache( glue, [new KeyValuePair<string, uint>( messageTypeName, cacheId )] );
		glue.StringCachingEnabled = true;

		using var stream = new MemoryStream();

		// When:
		NetworkMessage.WriteToStream( glue, stream, new CachedTypeNetworkMessage()
		{
			Property1 = property1,
			Property2 = property2
		} );

		// Then:
		stream.Position = 0;
		using var reader = new BinaryReader( stream, Encoding.Default );

		Assert.True( reader.ReadBoolean() );
		Assert.Equal( cacheId, reader.ReadUInt32() );
		Assert.Equal( property1, reader.ReadSingle() );
		Assert.Equal( property2, reader.ReadString() );
		Assert.Equal( stream.Length, stream.Position );
	}

	[Fact]
	public void WriteToStreamCachedStringCacheDisabled()
	{
		// Given:
		const float property1 = 123;
		const string property2 = "Hello, World!";
		var messageType = typeof( CachedTypeNetworkMessage );
		var messageTypeName = messageType.FullName ?? messageType.Name;
		using var stream = new MemoryStream();

		// When:
		NetworkMessage.WriteToStream( new ServerGlue(), stream, new CachedTypeNetworkMessage()
		{
			Property1 = property1,
			Property2 = property2
		} );

		// Then:
		stream.Position = 0;
		using var reader = new BinaryReader( stream, Encoding.Default );

		Assert.False( reader.ReadBoolean() );
		Assert.Equal( messageTypeName, reader.ReadString() );
		Assert.Equal( property1, reader.ReadSingle() );
		Assert.Equal( property2, reader.ReadString() );
		Assert.Equal( stream.Length, stream.Position );
	}

	[Fact]
	public void WriteToStreamUncached()
	{
		// Given:
		const float property1 = 123;
		const string property2 = "Hello, World!";
		var messageType = typeof( UncachedTypeNetworkMessage );
		var messageTypeName = messageType.FullName ?? messageType.Name;
		using var stream = new MemoryStream();

		// When:
		NetworkMessage.WriteToStream( new ServerGlue(), stream, new UncachedTypeNetworkMessage()
		{
			Property1 = property1,
			Property2 = property2
		} );

		// Then:
		stream.Position = 0;
		using var reader = new BinaryReader( stream, Encoding.Default );

		Assert.False( reader.ReadBoolean() );
		Assert.Equal( messageTypeName, reader.ReadString() );
		Assert.Equal( property1, reader.ReadSingle() );
		Assert.Equal( property2, reader.ReadString() );
		Assert.Equal( stream.Length, stream.Position );
	}

	[Fact]
	public void WriteToStreamThrowsOnNullGlue()
	{
		// Given:
		const string writeToStreamParameterName = "glue";

		// When:
		static void Execute()
		{
			NetworkMessage.WriteToStream( null!, Stream.Null, new CachedTypeNetworkMessage() );
		}

		// Then:
		Assert.Throws<ArgumentNullException>( writeToStreamParameterName, Execute );
	}

	[Fact]
	public void WriteToStreamThrowsOnNullStream()
	{
		// Given:
		const string writeToStreamParameterName = "stream";

		// When:
		static void Execute()
		{
			NetworkMessage.WriteToStream( new ServerGlue(), null!, new CachedTypeNetworkMessage() );
		}

		// Then:
		Assert.Throws<ArgumentNullException>( writeToStreamParameterName, Execute );
	}

	[Fact]
	public void WriteToStreamThrowsOnNullMessage()
	{
		// Given:
		const string writeToStreamParameterName = "message";

		// When:
		static void Execute()
		{
			NetworkMessage.WriteToStream( new ServerGlue(), Stream.Null, null! );
		}

		// Then:
		Assert.Throws<ArgumentNullException>( writeToStreamParameterName, Execute );
	}

	[Fact]
	public void WriteToStreamThrowsOnNullEncoding()
	{
		// Given:
		const string writeToStreamParameterName = "encoding";

		// When:
		static void Execute()
		{
			NetworkMessage.WriteToStream( new ServerGlue(), Stream.Null, new CachedTypeNetworkMessage(), null! );
		}

		// Then:
		Assert.Throws<ArgumentNullException>( writeToStreamParameterName, Execute );
	}

	[Fact]
	public void WriteToStreamThrowsOnNonWritableStream()
	{
		// Given:
		const string writeToStreamParameterName = "stream";

		// When:
		static void Execute()
		{
			NetworkMessage.WriteToStream( new ServerGlue(), new NonWritableStream(), new CachedTypeNetworkMessage() );
		}

		// Then:
		Assert.Throws<ArgumentException>( writeToStreamParameterName, Execute );
	}
	#endregion
}

file class CachedTypeNetworkMessage : NetworkMessage
{
	public override bool CacheTypeString => true;

	public float Property1 { get; set; }
	public string Property2 { get; set; } = string.Empty;

	public override void Deserialize( NetworkMessageReader reader )
	{
		Property1 = reader.ReadSingle();
		Property2 = reader.ReadString();
	}

	public override void Serialize( NetworkMessageWriter writer )
	{
		writer.Write( Property1 );
		writer.Write( Property2 );
	}
}

file class UncachedTypeNetworkMessage : NetworkMessage
{
	public override bool CacheTypeString => false;

	public float Property1 { get; set; }
	public string Property2 { get; set; } = string.Empty;

	public override void Deserialize( NetworkMessageReader reader )
	{
		Property1 = reader.ReadSingle();
		Property2 = reader.ReadString();
	}

	public override void Serialize( NetworkMessageWriter writer )
	{
		writer.Write( Property1 );
		writer.Write( Property2 );
	}
}

file class NonWritableStream : Stream
{
	public override bool CanRead => throw new NotImplementedException();

	public override bool CanSeek => throw new NotImplementedException();

	public override bool CanWrite => false;

	public override long Length => throw new NotImplementedException();

	public override long Position { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

	public override void Flush()
	{
		throw new NotImplementedException();
	}

	public override int Read( byte[] buffer, int offset, int count )
	{
		throw new NotImplementedException();
	}

	public override long Seek( long offset, SeekOrigin origin )
	{
		throw new NotImplementedException();
	}

	public override void SetLength( long value )
	{
		throw new NotImplementedException();
	}

	public override void Write( byte[] buffer, int offset, int count )
	{
		throw new NotImplementedException();
	}
}

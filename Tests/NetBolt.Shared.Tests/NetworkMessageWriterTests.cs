﻿using NetBolt.Messaging;
using NetBolt.Shared.Extensions;
using NetBolt.Tests.Shared.Mocks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Xunit;

namespace NetBolt.Tests.Shared;

public sealed class NetworkMessageWriterTests
{
	#region Test data
	public static TheoryData<string, Encoding> StringsAndEncodings()
	{
		return new TheoryData<string, Encoding>
		{
			{ "Hello, World!", Encoding.ASCII },
			{ "Hello, World!", Encoding.BigEndianUnicode },
			{ "Hello, World!", Encoding.Latin1 },
			{ "Hello, World!", Encoding.Unicode },
			{ "Hello, World!", Encoding.UTF32 },
			{ "Hello, World!", Encoding.UTF8 }
		};
	}

	public static TheoryData<Type, Encoding> TypesAndEncodings()
	{
		return new TheoryData<Type, Encoding>
		{
			{ typeof( object ), Encoding.ASCII },
			{ typeof( object ), Encoding.BigEndianUnicode },
			{ typeof( object ), Encoding.Latin1 },
			{ typeof( object ), Encoding.Unicode },
			{ typeof( object ), Encoding.UTF32 },
			{ typeof( object ), Encoding.UTF8 }
		};
	}
	#endregion

	#region WriteCacheString
	#region Cache enabled
	[Fact]
	public void WriteCacheStringCacheEnabled()
	{
		// Given:
		const string stringToWrite = "Hello, World!";

		var cacheEnabledGlue = new ServerGlue
		{
			StringCachingEnabled = true,
			StringCache = new StringCache()
		};
		var cacheId = cacheEnabledGlue.StringCache.Add( stringToWrite );

		using var writeStream = new MemoryStream();
		using var writer = new NetworkMessageWriter( writeStream, cacheEnabledGlue );

		// When:
		writer.WriteCacheString( stringToWrite );

		// Then:
		var data = writeStream.ToArray();
		using var readStream = new MemoryStream( data );
		using var reader = new BinaryReader( readStream );

		Assert.Equal( sizeof( bool ) + sizeof( uint ), data.Length );
		Assert.True( reader.ReadBoolean() );
		Assert.Equal( cacheId, reader.ReadUInt32() );
	}

	[Fact]
	public void WriteCacheStringThrowsOnNotCachedString()
	{
		// Given:
		const string stringToWrite = "Hello, World!";

		var cacheEnabledGlue = new ServerGlue
		{
			StringCachingEnabled = true,
			StringCache = new StringCache()
		};

		using var stream = new MemoryStream();
		using var writer = new NetworkMessageWriter( stream, cacheEnabledGlue );

		// When:
		void Execute()
		{
			writer.WriteCacheString( stringToWrite );
		}

		// Then:
		Assert.Throws<KeyNotFoundException>( Execute );
	}
	#endregion

	#region Cache disabled
	[Theory]
	[MemberData( nameof( StringsAndEncodings ) )]
	public void WriteCacheStringCacheDisabled( string stringToWrite, Encoding encoding )
	{
		// Given:
		var cacheDisabledGlue = new ServerGlue();

		using var writeStream = new MemoryStream();
		using var writer = new NetworkMessageWriter( writeStream, encoding, cacheDisabledGlue );

		// When:
		writer.WriteCacheString( stringToWrite );

		// Then:
		var data = writeStream.ToArray();
		using var readStream = new MemoryStream( data );
		using var reader = new BinaryReader( readStream, encoding );

		// cache bool + encoding byte count + string null terminator.
		var expectedSize = sizeof( bool ) + encoding.GetByteCount( stringToWrite ) + 1;
		Assert.Equal( expectedSize, data.Length );
		Assert.False( reader.ReadBoolean() );
		Assert.Equal( stringToWrite, reader.ReadString() );
	}
	#endregion

	[Fact]
	public void WriteCacheStringThrowsOnNullString()
	{
		// Given:
		const string stringToWrite = null!;
		const string writeCacheStringParameterName = "str";
		using var writer = new NetworkMessageWriter( Stream.Null, new ServerGlue() );

		// When:
		void Execute()
		{
			writer.WriteCacheString( stringToWrite );
		}

		// Then:
		Assert.Throws<ArgumentNullException>( writeCacheStringParameterName, Execute );
	}
	#endregion

	#region WriteType
	#region Cache enabled
	[Fact]
	public void WriteTypeCacheEnabled()
	{
		// Given:
		var typeToWrite = typeof( object );

		var cacheEnabledGlue = new ServerGlue
		{
			StringCachingEnabled = true,
			StringCache = new StringCache()
		};
		var cacheId = cacheEnabledGlue.StringCache.Add( typeToWrite );

		using var writeStream = new MemoryStream();
		using var writer = new NetworkMessageWriter( writeStream, cacheEnabledGlue );

		// When:
		writer.Write( typeToWrite );

		// Then:
		var data = writeStream.ToArray();
		using var readStream = new MemoryStream( data );
		using var reader = new BinaryReader( readStream );

		Assert.Equal( sizeof( bool ) + sizeof( uint ), data.Length );
		Assert.True( reader.ReadBoolean() );
		Assert.Equal( cacheId, reader.ReadUInt32() );
	}

	[Fact]
	public void WriteTypeThrowsOnNotCachedType()
	{
		// Given:
		var typeToWrite = typeof( object );

		var cacheEnabledGlue = new ServerGlue
		{
			StringCachingEnabled = true,
			StringCache = new StringCache()
		};

		using var stream = new MemoryStream();
		using var writer = new NetworkMessageWriter( stream, cacheEnabledGlue );

		// When:
		void Execute()
		{
			writer.Write( typeToWrite );
		}

		// Then:
		Assert.Throws<KeyNotFoundException>( Execute );
	}
	#endregion

	#region Cache disabled
	[Theory]
	[MemberData( nameof( TypesAndEncodings ) )]
	public void WriteTypeCacheDisabled( Type typeToWrite, Encoding encoding )
	{
		// Given:
		var typeString = typeToWrite.FullName ?? typeToWrite.Name;
		var cacheDisabledGlue = new ServerGlue();

		using var writeStream = new MemoryStream();
		using var writer = new NetworkMessageWriter( writeStream, encoding, cacheDisabledGlue );

		// When:
		writer.Write( typeToWrite );

		// Then:
		var data = writeStream.ToArray();
		using var readStream = new MemoryStream( data );
		using var reader = new BinaryReader( readStream, encoding );

		// cache bool + encoding byte count + string null terminator.
		var expectedSize = sizeof( bool ) + encoding.GetByteCount( typeString ) + 1;
		Assert.Equal( expectedSize, data.Length );
		Assert.False( reader.ReadBoolean() );
		Assert.Equal( typeString, reader.ReadString() );
	}
	#endregion

	[Fact]
	public void WriteTypeThrowsOnNullType()
	{
		// Given:
		Type typeToWrite = null!;
		const string writeTypeParameterName = "type";
		using var writer = new NetworkMessageWriter( Stream.Null, new ServerGlue() );

		// When:
		void Execute()
		{
			writer.Write( typeToWrite );
		}

		// Then:
		Assert.Throws<ArgumentNullException>( writeTypeParameterName, Execute );
	}
	#endregion
}

using NetBolt.Messaging;
using NetBolt.Tests.Shared.Mocks;
using System;
using System.Collections.Generic;
using Xunit;

namespace NetBolt.Tests.Shared;

public class NetworkMessageReaderTests
{
	#region ReadType
	[Fact]
	public void ReadType()
	{
		// Given:
		var cachedType = typeof( object );
		var cachedTypeStr = cachedType.FullName ?? cachedType.Name;
		const uint cacheId = 1;

		using var stream = TestUtility.CreateStream( testDataWriter =>
		{
			testDataWriter.Write( true );
			testDataWriter.Write( cacheId );
		} );

		var glue = new ClientGlue
		{
			StringCache = new StringCache( [new KeyValuePair<string, uint>( cachedTypeStr, cacheId )] ),
			StringCachingEnabled = true
		};
		using var reader = new NetworkMessageReader( stream, glue );

		// When:
		var type = reader.ReadType();

		// Then:
		Assert.Equal( cachedType, type );
	}

	[Fact]
	public void ReadTypeThrowsOnUnknownType()
	{
		// Given:
		const string cachedBadTypeStr = "Some.Unknown.Type";
		const uint cacheId = 1;

		using var stream = TestUtility.CreateStream( testDataWriter =>
		{
			testDataWriter.Write( true );
			testDataWriter.Write( cacheId );
		} );

		var glue = new ClientGlue
		{
			StringCache = new StringCache( [new KeyValuePair<string, uint>( cachedBadTypeStr, cacheId )] ),
			StringCachingEnabled = true
		};
		using var reader = new NetworkMessageReader( stream, glue );

		// When:
		void Execute()
		{
			var type = reader.ReadType();
		}

		// Then:
		Assert.Throws<InvalidOperationException>( Execute );
	}
	#endregion

	#region ReadCacheString
	[Fact]
	public void ReadCacheString()
	{
		// Given:
		const string cacheStr = "Hello, World!";
		const uint cacheId = 1;

		using var stream = TestUtility.CreateStream( testDataWriter =>
		{
			testDataWriter.Write( true );
			testDataWriter.Write( cacheId );
		} );

		var glue = new ClientGlue
		{
			StringCache = new StringCache( [new KeyValuePair<string, uint>( cacheStr, cacheId )] ),
			StringCachingEnabled = true
		};
		using var reader = new NetworkMessageReader( stream, glue );

		// When:
		var str = reader.ReadCacheString();

		// Then:
		Assert.Equal( cacheStr, str );
	}

	[Fact]
	public void ReadCacheStringUncached()
	{
		// Given:
		const string uncachedStr = "Hello, World!";

		using var stream = TestUtility.CreateStream( testDataWriter =>
		{
			testDataWriter.Write( false );
			testDataWriter.Write( uncachedStr );
		} );

		using var reader = new NetworkMessageReader( stream, new ClientGlue() );

		// When:
		var str = reader.ReadCacheString();

		// Then:
		Assert.Equal( uncachedStr, str );
	}

	[Fact]
	public void ReadCacheStringThrowsOnDisabledCache()
	{
		// Given:
		using var stream = TestUtility.CreateStream( testDataWriter =>
		{
			testDataWriter.Write( true );
			testDataWriter.Write( (uint)1 );
		} );

		using var reader = new NetworkMessageReader( stream, new ClientGlue() );

		// When:
		void Execute()
		{
			var str = reader.ReadCacheString();
		}

		// Then:
		Assert.Throws<InvalidOperationException>( Execute );
	}

	[Fact]
	public void ReadCacheStringThrowsOnInvalidId()
	{
		// Given:
		using var stream = TestUtility.CreateStream( testDataWriter =>
		{
			testDataWriter.Write( true );
			testDataWriter.Write( (uint)2 );
		} );

		var glue = new ClientGlue
		{
			StringCache = new StringCache(),
			StringCachingEnabled = true
		};
		using var reader = new NetworkMessageReader( stream, glue );

		// When:
		void Execute()
		{
			var str = reader.ReadCacheString();
		}

		// Then:
		Assert.Throws<KeyNotFoundException>( Execute );
	}
	#endregion
}

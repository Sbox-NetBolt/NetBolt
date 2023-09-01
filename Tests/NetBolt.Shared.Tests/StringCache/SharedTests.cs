using NetBolt.Glue;
using System;
using Xunit;

namespace NetBolt.Tests.Shared.StringCache;

public sealed class SharedTests
{
	// Using the server glue so that the add calls can function.
	private static readonly INetBoltGlue glue = new ServerGlue();

	#region TryGetId
	[Fact]
	public void TryGetIdFromCache()
	{
		// Given:
		const string stringToCache = "Hello, World!";
		var stringCache = new NetBolt.StringCache( glue );
		var cacheId = stringCache.Add( stringToCache );

		// When:
		var isCached = stringCache.TryGetId( stringToCache, out var cachedId );

		// Then:
		Assert.True( isCached );
		Assert.Equal( cacheId, cachedId );
	}

	[Fact]
	public void TryGetIdFromCacheFail()
	{
		// Given:
		const string stringToCache = "Hello, World!";
		var stringCache = new NetBolt.StringCache( glue );

		// When:
		var isCached = stringCache.TryGetId( stringToCache, out var cachedId );

		// Then:
		Assert.False( isCached );
		Assert.Null( cachedId );
	}

	[Fact]
	public void TryGetIdThrowsOnNullString()
	{
		// Given:
		const string stringToCache = null!;
		const string tryGetIdStringParameterName = "str";
		var stringCache = new NetBolt.StringCache( glue );

		// When:
		void Execute()
		{
			stringCache.TryGetId( stringToCache, out var cachedId );
		}


		// Then:
		Assert.Throws<ArgumentNullException>( tryGetIdStringParameterName, Execute );
	}
	#endregion

	#region TryGetString
	[Fact]
	public void TryGetStringFromCache()
	{
		// Given:
		const string stringToCache = "Hello, World!";
		var stringCache = new NetBolt.StringCache( glue );
		var cacheId = stringCache.Add( stringToCache );

		// When:
		var isCached = stringCache.TryGetString( cacheId, out var cachedString );

		// Then:
		Assert.True( isCached );
		Assert.Equal( stringToCache, cachedString );
	}

	[Fact]
	public void TryGetStringFromCacheFail()
	{
		// Given:
		const uint idToLookup = 1;
		var stringCache = new NetBolt.StringCache( glue );

		// When:
		var isCached = stringCache.TryGetString( idToLookup, out var cachedString );

		// Then:
		Assert.False( isCached );
		Assert.Null( cachedString );
	}
	#endregion
}

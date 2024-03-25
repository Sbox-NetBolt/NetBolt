using NetBolt.Exceptions;
using System.Collections.Generic;
using System.Collections.Immutable;
using Xunit;

namespace NetBolt.Tests.Shared.StringCacheTests;

public sealed class ClientTests
{
	[Fact]
	public void InitCache()
	{
		// Given:
		const string entryString = "Hello, World!";
		const uint entryId = 1;
		var entries = ImmutableArray.Create( new KeyValuePair<string, uint>( entryString, entryId ) );
		var stringCache = new StringCache( entries );

		// Then:
		var (cachedString, cachedId) = Assert.Single( stringCache.Entries );
		Assert.Equal( entryString, cachedString );
		Assert.Equal( entryId, cachedId );
	}

	[Fact]
	public void SwapEntries()
	{
		// Given:
		const string entryString = "Hello, World!";
		const uint entryId = 1;
		var stringCache = new StringCache();
		var entries = ImmutableArray.Create( new KeyValuePair<string, uint>( entryString, entryId ) );

		// When:
		stringCache.Swap( entries );

		// Then:
		var (cachedString, cachedId) = Assert.Single( stringCache.Entries );
		Assert.Equal( entryString, cachedString );
		Assert.Equal( entryId, cachedId );
	}
}

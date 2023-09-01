using NetBolt.Exceptions;
using NetBolt.Glue;
using System.Collections.Generic;
using System.Collections.Immutable;
using Xunit;

namespace NetBolt.Tests.Shared.StringCache;

public sealed class ClientTests
{
	private static readonly INetBoltGlue glue = new ClientGlue();

	[Fact]
	public void SwapEntries()
	{
		// Given:
		const string entryString = "Hello, World!";
		const uint entryId = 1;
		var stringCache = new NetBolt.StringCache( glue );
		var entries = ImmutableArray.Create( new KeyValuePair<string, uint>( entryString, entryId ) );

		// When:
		stringCache.Swap( entries );

		// Then:
		var (cachedString, cachedId) = Assert.Single( stringCache.Entries );
		Assert.Equal( entryString, cachedString );
		Assert.Equal( entryId, cachedId );
	}

	[Fact]
	public void AddThrowsOnClient()
	{
		// Given:
		const string stringToCache = "Hello, World!";
		var stringCache = new NetBolt.StringCache( glue );

		// When:
		void Execute()
		{
			stringCache.Add( stringToCache );
		}

		// Then:
		Assert.Throws<RealmException>( Execute );
	}

	[Fact]
	public void RemoveThrowsOnClient()
	{
		// Given:
		const string stringToCache = "Hello, World!";
		var stringCache = new NetBolt.StringCache( glue );

		// When:
		void Execute()
		{
			stringCache.Remove( stringToCache );
		}

		// Then:
		Assert.Throws<RealmException>( Execute );
	}
}

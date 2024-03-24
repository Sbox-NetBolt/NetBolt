using NetBolt.Exceptions;
using NetBolt.Shared.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Xunit;

namespace NetBolt.Tests.Shared.StringCacheTests;

public sealed class ServerTests
{
	[Fact]
	public void ConstructFromEntriesThrowsOnServer()
	{
		// When:
		static void Execute()
		{
			var stringCache = new StringCache( ImmutableArray<KeyValuePair<string, uint>>.Empty );
		}

		// Then:
		Assert.Throws<RealmException>( Execute );
	}

	[Fact]
	public void SwapThrowsOnServer()
	{
		// Given:
		var stringCache = new StringCache();

		// When:
		void Execute()
		{
			stringCache.Swap( ImmutableArray<KeyValuePair<string, uint>>.Empty );
		}

		// Then:
		Assert.Throws<RealmException>( Execute );
	}

	#region Add
	#region Add( string )
	[Fact]
	public void AddStringToCache()
	{
		// Given:
		const string stringToCache = "Hello, World!";
		var stringCache = new StringCache();

		// When:
		var cacheId = stringCache.Add( stringToCache );

		// Then:
		Assert.Equal( 1u, cacheId );
		var (cachedString, cachedId) = Assert.Single( stringCache.Entries );
		Assert.Equal( stringToCache, cachedString );
		Assert.Equal( 1u, cachedId );
	}

	[Fact]
	public void AddThrowsOnNullString()
	{
		// Given:
		const string stringToCache = null!;
		const string addStringParameterName = "str";
		var stringCache = new StringCache();

		// When:
		void Execute()
		{
			stringCache.Add( stringToCache );
		}

		// Then:
		Assert.Throws<ArgumentNullException>( addStringParameterName, Execute );
	}

	[Fact]
	public void AddThrowsOnDuplicateString()
	{
		// Given:
		const string stringToCache = "Hello, World!";
		var stringCache = new StringCache(	);
		stringCache.Add( stringToCache );

		// When:
		void Execute()
		{
			stringCache.Add( stringToCache );
		}

		// Then:
		Assert.Throws<ArgumentException>( Execute );
	}

	[Fact]
	public void AddInvokesOnChangedEvent()
	{
		// Given:
		const string stringToCache = "Hello, World!";
		var stringCache = new StringCache();
		var hasStringCacheChanged = false;
		stringCache.OnChanged += OnChanged;

		void OnChanged( StringCache cache )
		{
			hasStringCacheChanged = true;
		}

		// When:
		stringCache.Add( stringToCache );

		// Then:
		Assert.True( hasStringCacheChanged );
	}
	#endregion

	#region Add( Type ) extension
	[Fact]
	public void AddTypeToCache()
	{
		// Given:
		var typeToCache = typeof( object );
		var stringCache = new StringCache();

		// When:
		var cacheId = stringCache.Add( typeToCache );

		// Then:
		Assert.Equal( 1u, cacheId );
		var (cachedTypeString, cachedId) = Assert.Single( stringCache.Entries );
	}

	[Fact]
	public void AddThrowsOnNullType()
	{
		// Given:
		Type typeToCache = null!;
		const string addTypeParameterName = "type";
		var stringCache = new StringCache();

		// When:
		void Execute()
		{
			stringCache.Add( typeToCache );
		}

		// Then:
		Assert.Throws<ArgumentNullException>( addTypeParameterName, Execute );
	}
	#endregion
	#endregion

	#region Remove
	#region Remove( string )
	[Fact]
	public void RemoveStringFromCache()
	{
		// Given:
		const string stringToCache = "Hello, World!";
		var stringCache = new StringCache();
		stringCache.Add( stringToCache );

		// When:
		stringCache.Remove( stringToCache );

		// Then:
		Assert.Empty( stringCache.Entries );
	}

	[Fact]
	public void RemoveThrowsOnNullString()
	{
		// Given:
		const string stringToCache = null!;
		const string removeStringParameterName = "str";
		var stringCache = new StringCache();

		// When:
		void Execute()
		{
			stringCache.Remove( stringToCache );
		}

		// Then:
		Assert.Throws<ArgumentNullException>( removeStringParameterName, Execute );
	}

	[Fact]
	public void RemoveThrowsOnNotCachedString()
	{
		// Given:
		const string stringToCache = "Hello, World!";
		const string removeStringParameterName = "str";
		var stringCache = new StringCache();

		// When:
		void Execute()
		{
			stringCache.Remove( stringToCache );
		}

		// Then:
		Assert.Throws<ArgumentException>( removeStringParameterName, Execute );
	}

	[Fact]
	public void RemoveInvokesOnChangedEvent()
	{
		// Given:
		const string stringToCache = "Hello, World!";
		var stringCache = new StringCache();
		stringCache.Add( stringToCache );
		var hasStringCacheChanged = false;
		stringCache.OnChanged += OnChanged;

		void OnChanged( StringCache cache )
		{
			hasStringCacheChanged = true;
		}

		// When:
		stringCache.Remove( stringToCache );

		// Then:
		Assert.True( hasStringCacheChanged );
	}
	#endregion
	#region Remove( Type ) extension
	[Fact]
	public void RemoveTypeFromCache()
	{
		// Given:
		var typeToCache = typeof( object );
		var stringCache = new StringCache();
		stringCache.Add( typeToCache );

		// When:
		stringCache.Remove( typeToCache );

		// Then:
		Assert.Empty( stringCache.Entries );
	}

	[Fact]
	public void RemoveThrowsOnNullType()
	{
		// Given:
		Type typeToCache = null!;
		const string removeTypeParameterName = "type";
		var stringCache = new StringCache();

		// When:
		void Execute()
		{
			stringCache.Remove( typeToCache );
		}

		// Then:
		Assert.Throws<ArgumentNullException>( removeTypeParameterName, Execute );
	}
	#endregion
	#endregion
}

using NetBolt.Attributes;
using NetBolt.Exceptions;
using NetBolt.Glue;
using NetBolt.Glue.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace NetBolt;

public sealed class StringCache
{
	[ServerOnly]
	public delegate void ChangedHandler( StringCache cache );
	[ServerOnly]
	public event ChangedHandler? OnChanged;

	public ImmutableArray<KeyValuePair<string, uint>> Entries = ImmutableArray<KeyValuePair<string, uint>>.Empty;

	[ServerOnly]
	private uint CurrentId = 1;

	private ConcurrentDictionary<string, uint> strings = new();
	private ConcurrentDictionary<uint, string> stringsReversed = new();

	private readonly INetBoltGlue glue;

	public StringCache( INetBoltGlue glue )
	{
		this.glue = glue;
	}

	[ClientOnly]
	public StringCache( INetBoltGlue glue, in ImmutableArray<KeyValuePair<string, uint>> entries ) : this( glue )
	{
		RealmException.ThrowIfNot( glue, Realm.Client );

		for ( var i = 0; i < entries.Length; i++ )
		{
			var entry = entries[i];
			strings.TryAdd( entry.Key, entry.Value );
			stringsReversed.TryAdd( entry.Value, entry.Key );
		}

		Entries = entries;
	}

	[ClientOnly]
	public void Swap( in ImmutableArray<KeyValuePair<string, uint>> entries )
	{
		RealmException.ThrowIfNot( glue, Realm.Client );

		var strings = new ConcurrentDictionary<string, uint>();
		var stringsReversed = new ConcurrentDictionary<uint, string>();

		for ( var i = 0; i < entries.Length; i++ )
		{
			var entry = entries[i];
			strings.TryAdd( entry.Key, entry.Value );
			stringsReversed.TryAdd( entry.Value, entry.Key );
		}

		this.strings = strings;
		this.stringsReversed = stringsReversed;

		Entries = entries;
	}

	[ServerOnly]
	public uint Add( string str )
	{
		RealmException.ThrowIfNot( glue, Realm.Server );
		ArgumentNullException.ThrowIfNull( str, nameof( str ) );

		if ( strings.ContainsKey( str ) )
			throw new ArgumentException( $"The string \"{str}\" is already cached", nameof( str ) );

		var id = CurrentId++;
		strings.TryAdd( str, id );
		stringsReversed.TryAdd( id, str );
		Entries = strings.ToImmutableArray();

		OnChanged?.Invoke( this );
		return id;
	}

	[ServerOnly]
	public void Remove( string str )
	{
		RealmException.ThrowIfNot( glue, Realm.Server );
		ArgumentNullException.ThrowIfNull( str, nameof( str ) );

		if ( !strings.ContainsKey( str ) )
			throw new ArgumentException( $"The string \"{str}\" is not cached", nameof( str ) );

		strings.TryRemove( str, out var id );
		stringsReversed.TryRemove( id, out _ );
		Entries = strings.ToImmutableArray();

		OnChanged?.Invoke( this );
	}

	public bool TryGetId( string str, [NotNullWhen( true )] out uint? id )
	{
		ArgumentNullException.ThrowIfNull( str, nameof( str ) );

		if ( strings.TryGetValue( str, out var cachedId ) )
		{
			id = cachedId;
			return true;
		}

		id = null;
		return false;
	}

	public bool TryGetString( uint id, [NotNullWhen( true )] out string? str )
	{
		if ( stringsReversed.TryGetValue( id, out var cachedStr ) )
		{
			str = cachedStr;
			return true;
		}

		str = null;
		return false;
	}
}

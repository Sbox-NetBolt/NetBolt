using NetBolt.Attributes;
using NetBolt.Exceptions;
using NetBolt.Glue;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace NetBolt.Messaging.Messages;

public sealed class StringCacheUpdateMessage : NetworkMessage
{
	public override bool CacheTypeString => false;

	public ImmutableArray<KeyValuePair<string, uint>> Entries { get; private set; } = ImmutableArray<KeyValuePair<string, uint>>.Empty;

	private readonly INetBoltGlue glue;

	[ClientOnly, ForReplication]
	public StringCacheUpdateMessage( INetBoltGlue glue )
	{
		this.glue = glue;
		RealmException.ThrowIfNot( glue, Realm.Client );
	}

	[ServerOnly]
	public StringCacheUpdateMessage( INetBoltGlue glue, in ImmutableArray<KeyValuePair<string, uint>> entries )
	{
		this.glue = glue;
		RealmException.ThrowIfNot( glue, Realm.Server );
		Entries = entries;
	}

	[ServerOnly]
	public override void Serialize( NetworkMessageWriter writer )
	{
		RealmException.ThrowIfNot( glue, Realm.Server );

		writer.Write( Entries.Length );
		for ( var i = 0; i < Entries.Length; i++ )
		{
			var entry = Entries[i];
			writer.Write( entry.Key );
			writer.Write( entry.Value );
		}
	}

	[ClientOnly]
	public override void Deserialize( NetworkMessageReader reader )
	{
		RealmException.ThrowIfNot( glue, Realm.Client );

		var entries = ImmutableArray.CreateBuilder<KeyValuePair<string, uint>>( reader.ReadInt32() );

		for ( var i = 0; i < entries.Capacity; i++ )
		{
			var str = reader.ReadString();
			var id = reader.ReadUInt32();
			entries.Add( new KeyValuePair<string, uint>( str, id ) );
		}

		Entries = entries.MoveToImmutable();
	}
}

using NetBolt.Glue;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace NetBolt.Messaging;

public class NetworkMessageReader : BinaryReader
{
	public INetBoltGlue Glue { get; }

	public NetworkMessageReader( Stream input, INetBoltGlue glue ) : base( input )
	{
		Glue = glue;
	}

	public NetworkMessageReader( Stream input, Encoding encoding, INetBoltGlue glue ) : base( input, encoding )
	{
		Glue = glue;
	}

	public NetworkMessageReader( Stream input, Encoding encoding, bool leaveOpen, INetBoltGlue glue ) : base( input, encoding, leaveOpen )
	{
		Glue = glue;
	}

	public virtual Type ReadType()
	{
		var typeStr = ReadCacheString();

		var type = Glue.GetType( typeStr );
		if ( type is null )
			throw new InvalidOperationException( $"No type with the name \"{typeStr}\" was found" );

		return type;
	}

	public virtual string ReadCacheString()
	{
		if ( !ReadBoolean() )
			return ReadString();

		if ( !Glue.StringCachingEnabled )
			throw new InvalidOperationException( "String caching is disabled" );

		var id = ReadUInt32();
		if ( !Glue.StringCache.TryGetString( id, out var str ) )
			throw new KeyNotFoundException( $"There is no ID {id} in the string cache" );

		return str;
	}
}

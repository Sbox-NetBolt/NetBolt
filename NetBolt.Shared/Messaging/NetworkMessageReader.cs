using NetBolt.Glue;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace NetBolt.Messaging;

public class NetworkMessageReader : BinaryReader
{
	public INetBoltGlue Glue { get; }

	internal NetworkMessageReader( Stream input, INetBoltGlue glue ) : base( input )
	{
		Glue = glue;
	}

	internal NetworkMessageReader( Stream input, Encoding encoding, INetBoltGlue glue ) : base( input, encoding )
	{
		Glue = glue;
	}

	internal NetworkMessageReader( Stream input, Encoding encoding, bool leaveOpen, INetBoltGlue glue ) : base( input, encoding, leaveOpen )
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

		var id = ReadUInt32();
		if ( !Glue.StringCache.TryGetString( id, out var str ) )
		{
			Glue.StringCache.Dump();
			throw new KeyNotFoundException( $"There is no ID {id} in the string cache" );
		}

		return str;
	}
}

using NetBolt.Glue;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace NetBolt.Messaging;

public class NetworkMessageWriter : BinaryWriter
{
	public INetBoltGlue Glue { get; }

	public NetworkMessageWriter( Stream output, INetBoltGlue glue ) : base( output )
	{
		Glue = glue;
	}

	public NetworkMessageWriter( Stream output, Encoding encoding, INetBoltGlue glue ) : base( output, encoding )
	{
		Glue = glue;
	}

	public NetworkMessageWriter( Stream output, Encoding encoding, bool leaveOpen, INetBoltGlue glue ) : base( output, encoding, leaveOpen )
	{
		Glue = glue;
	}

	public virtual void Write( Type type )
	{
		ArgumentNullException.ThrowIfNull( type, nameof( type ) );

		WriteCacheString( type.FullName ?? type.Name );
	}

	public virtual void WriteCacheString( string str )
	{
		ArgumentNullException.ThrowIfNull( str, nameof( str ) );

		if ( !Glue.StringCachingEnabled )
		{
			Write( false );
			Write( str );
			return;
		}

		if ( !Glue.StringCache.TryGetId( str, out var id ) )
		{
			Glue.StringCache.Dump();
			throw new KeyNotFoundException( $"There is no string \"{str}\" in the string cache" );
		}

		Write( true );
		Write( id.Value );
	}
}

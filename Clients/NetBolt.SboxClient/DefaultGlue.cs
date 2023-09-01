using NetBolt.Exceptions;
using NetBolt.Glue;
using NetBolt.Glue.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NetBolt.Client;

internal sealed class DefaultGlue : INetBoltGlue
{
	public Realm Realm => Realm.Client;

	public ILogger Logger { get; }

	public IClientConnection ClientConnection => throw new NotImplementedException();
	public IServerHost ServerHost => throw new RealmException( $"{nameof( ServerHost )} cannot be used on the client realm" );

	public bool StringCachingEnabled => StringCache.Entries.IsEmpty;

	public StringCache StringCache { get; }

	internal DefaultGlue( ILogger logger )
	{
		Logger = logger;
		StringCache = new StringCache( this );
	}

	public object? CreateInstance( Type type )
	{
		try
		{
			return TypeLibrary.Create<object?>( type, new object?[] { this } );
		}
		catch ( Exception )
		{
			return TypeLibrary.Create<object?>( type );
		}
	}

	public Type? GetType( string typeName )
	{
		return TypeLibrary.GetType( typeName )?.TargetType;
	}

	public IEnumerable<Type> GetTypesAssignableTo( Type type ) => TypeLibrary.GetTypes( type )
		.Select( typeDescription => typeDescription.TargetType );
}

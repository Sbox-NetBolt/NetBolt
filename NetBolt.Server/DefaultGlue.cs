using NetBolt.Exceptions;
using NetBolt.Glue;
using NetBolt.Glue.Logging;
using System;
using System.Collections.Generic;

namespace NetBolt.Server;

internal sealed class DefaultGlue : INetBoltGlue
{
	public Realm Realm => Realm.Server;

	public ILogger Logger { get; }

	public IClientConnection ClientConnection => throw new RealmException( $"{nameof( ClientConnection )} cannot be used on the server realm" );
	public IServerHost ServerHost { get; }

	public bool StringCachingEnabled { get; }

	public StringCache StringCache { get; }

	internal DefaultGlue( ILogger logger, IServerHost serverHost, bool stringCachingEnabled )
	{
		Logger = logger;
		ServerHost = serverHost;
		StringCachingEnabled = stringCachingEnabled;
		StringCache = new( this );
	}

	public object? CreateInstance( Type type )
	{
		try
		{
			return Activator.CreateInstance( type, new object?[] { this } );
		}
		catch ( Exception )
		{
			return Activator.CreateInstance( type );
		}
	}

	public Type? GetType( string typeName )
	{
		foreach ( var assembly in AppDomain.CurrentDomain.GetAssemblies() )
		{
			var type = assembly.GetType( typeName );
			if ( type is null )
				continue;

			return type;
		}

		return null;
	}

	public IEnumerable<Type> GetTypesAssignableTo( Type type )
	{
		foreach ( var assembly in AppDomain.CurrentDomain.GetAssemblies() )
		{
			foreach ( var assemblyType in assembly.DefinedTypes )
			{
				if ( !assemblyType.IsAssignableTo( type ) )
					continue;

				yield return assemblyType;
			}
		}
	}
}

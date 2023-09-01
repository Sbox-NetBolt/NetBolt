using NetBolt.Exceptions;
using NetBolt.Glue;
using NetBolt.Glue.Logging;
using System;
using System.Collections.Generic;

namespace NetBolt.Tests.Shared.Mocks;

internal sealed class ServerGlue : INetBoltGlue
{
	public Realm Realm => Realm.Server;

	public ILogger Logger => NullLogger.Instance;

	public IClientConnection ClientConnection => throw new RealmException();

	public IServerHost ServerHost { get; init; }

	public bool StringCachingEnabled { get; init; }

	public NetBolt.StringCache StringCache { get; init; }

	internal ServerGlue( IServerHost? serverHost = null, NetBolt.StringCache? stringCache = null )
	{
		ServerHost = serverHost ?? new ServerHost();
		StringCachingEnabled = stringCache is not null;
		StringCache = stringCache ?? new NetBolt.StringCache( this );
	}

	public object? CreateInstance( Type type )
	{
		return Activator.CreateInstance( type );
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

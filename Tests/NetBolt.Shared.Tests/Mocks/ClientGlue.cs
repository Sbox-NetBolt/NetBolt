﻿using NetBolt.Exceptions;
using NetBolt.Glue;
using NetBolt.Glue.Logging;
using System;
using System.Collections.Generic;

namespace NetBolt.Tests.Shared.Mocks;

internal sealed class ClientGlue : INetBoltGlue
{
	public Realm Realm => Realm.Client;

	public ILogger Logger => NullLogger.Instance;

	public IClientConnection ClientConnection { get; internal set; }

	public IServerHost ServerHost => throw new RealmException();

	public bool StringCachingEnabled { get; internal set; }

	public StringCache StringCache { get; internal set; }

	internal ClientGlue( IClientConnection? clientConnection = null, StringCache? stringCache = null )
	{
		ClientConnection = clientConnection ?? new ClientConnection();
		StringCachingEnabled = stringCache is not null;
		StringCache = stringCache ?? new StringCache();
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

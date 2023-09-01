using NetBolt.Messaging;
using System;

namespace NetBolt.NetworkableExtension.Extensions;

public static class NetworkMessageReaderExtensions
{
	public static T ReadNetworkable<T>( this NetworkMessageReader reader ) where T : INetworkable
	{
		var networkableType = reader.ReadType();
		if ( !networkableType.IsAssignableTo( typeof( INetworkable ) ) )
			throw new InvalidOperationException( $"Received a type that is not assignable to {nameof( INetworkable )} ({networkableType})" );

		var networkable = (T)reader.Glue.CreateInstance( networkableType )!;
		networkable.Deserialize( reader );

		return networkable;
	}

	public static void ReadNetworkable<T>( this NetworkMessageReader reader, ref T networkable ) where T : INetworkable
	{
		ArgumentNullException.ThrowIfNull( networkable, nameof( networkable ) );

		networkable.Deserialize( reader );
	}

	/*public static void ReadNetworkableChanges<T>( this NetworkMessageReader reader, ref T networkable ) where T : INetworkable
	{
		ArgumentNullException.ThrowIfNull( networkable, nameof( networkable ) );

		networkable.DeserializeChanges( reader );
	}*/
}

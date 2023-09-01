using NetBolt.Messaging;
using System;

namespace NetBolt.NetworkableExtension.Extensions;

public static class NetworkMessageWriterExtensions
{
    public static void Write<T>( this NetworkMessageWriter writer, ref T networkable ) where T : INetworkable => Write( writer, ref networkable, false );
	public static void Write<T>( this NetworkMessageWriter writer, ref T networkable, bool typeless ) where T : INetworkable
	{
		ArgumentNullException.ThrowIfNull( networkable, nameof( networkable ) );

		if ( !typeless )
			writer.Write( networkable.GetType() );

		networkable.Serialize( writer );
	}

	public static void Write<T>( this NetworkMessageWriter writer, T networkable ) where T : class, INetworkable => Write( writer, networkable, false );
	public static void Write<T>( this NetworkMessageWriter writer, T networkable, bool typeless ) where T : class, INetworkable
	{
		ArgumentNullException.ThrowIfNull( networkable, nameof( networkable ) );

		if ( !typeless )
			writer.Write( networkable.GetType() );

		networkable.Serialize( writer );
	}

	/*public static void WriteChanges<T>( this NetworkMessageWriter writer, ref T networkable ) where T : INetworkable
	{
		ArgumentNullException.ThrowIfNull( networkable );

		networkable.SerializeChanges( writer );
	}*/
}

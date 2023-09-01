using NetBolt.Glue;
using System;

namespace NetBolt.NetworkableExtension;

public sealed class NetworkableExtensionOptions
{
	public static NetworkableExtensionOptions Default => new();

	public INetBoltGlue Glue { get; set; }
	public bool CacheNetworkableTypes { get; set; }

	public NetworkableExtensionOptions()
	{
	}

	public NetworkableExtensionOptions( NetworkableExtensionOptions other )
	{
		CacheNetworkableTypes = other.CacheNetworkableTypes;
	}

	public NetworkableExtensionOptions WithGlue( INetBoltGlue glue )
	{
		Glue = glue;
		return this;
	}

	public NetworkableExtensionOptions WithCacheNetworkableTypes( bool cacheNetworkableTypes )
	{
		CacheNetworkableTypes = cacheNetworkableTypes;
		return this;
	}

	public void Validate()
	{
		// Glue.
		ArgumentNullException.ThrowIfNull( Glue, nameof( Glue ) );
	}
}

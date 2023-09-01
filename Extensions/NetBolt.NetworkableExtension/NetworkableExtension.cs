using NetBolt.Glue;
using NetBolt.Shared;
using NetBolt.Shared.Extensions;

namespace NetBolt.NetworkableExtension;

public sealed class NetworkableExtension : IExtension
{
	public string Name => "Networkables";

	private readonly NetworkableExtensionOptions options;
	private readonly INetBoltGlue glue;
	private readonly bool cacheNetworkableTypes;

	public NetworkableExtension() : this( NetworkableExtensionOptions.Default )
	{
	}

	public NetworkableExtension( NetworkableExtensionOptions options )
	{
		this.options = options;

		glue = options.Glue;
		cacheNetworkableTypes = options.CacheNetworkableTypes;
	}

	public void Start()
	{
		if ( !cacheNetworkableTypes )
			return;

		foreach ( var type in glue.GetTypesAssignableTo<INetworkable>() )
		{
			if ( type.IsAbstract || type.IsInterface )
				continue;

			glue.StringCache.Add( type );
		}
	}

	public void Stop()
	{
	}

	public void ProcessEvents()
	{
	}
}

using NetBolt.Server;

namespace NetBolt.Extensions.Sbox;

public static class NetBoltServerOptionsExtensions
{
	public static NetBoltServerOptions AddSboxExtension( this NetBoltServerOptions options )
	{
		options.Extensions.AddExtension<SboxExtension>();
		return options;
	}

	public static NetBoltServerOptions AddSboxExtension( this NetBoltServerOptions options, SboxExtensionOptions extensionOptions )
	{
		options.Extensions.AddExtension( new SboxExtension( extensionOptions ) );
		return options;
	}
}

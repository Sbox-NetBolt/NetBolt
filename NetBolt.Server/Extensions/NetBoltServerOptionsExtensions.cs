namespace NetBolt.Server.Extensions;

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

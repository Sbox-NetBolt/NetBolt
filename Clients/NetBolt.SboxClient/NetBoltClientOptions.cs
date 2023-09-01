using NetBolt.Glue;
using NetBolt.Glue.Logging;
using System;
using System.Text;

namespace NetBolt.Client;

public sealed class NetBoltClientOptions
{
	public static NetBoltClientOptions Default => new();

	public bool AllowPartialMessages { get; set; }
	public INetBoltGlue? Glue { get; set; }
	public ILogger Logger { get; set; } = NullLogger.Instance;
	public int MaxMessageSize { get; set; } = 65536;
	public int MaxPartialMessageCount { get; set; } = 10;
	public Encoding NetworkMessageCharacterEncoding { get; set; } = Encoding.Default;

	public NetBoltClientOptions()
	{
	}

	public NetBoltClientOptions( NetBoltClientOptions options )
	{
		AllowPartialMessages = options.AllowPartialMessages;
		Glue = options.Glue;
		Logger = options.Logger;
		MaxMessageSize = options.MaxMessageSize;
		MaxPartialMessageCount = options.MaxPartialMessageCount;
		NetworkMessageCharacterEncoding = options.NetworkMessageCharacterEncoding;
	}

	public NetBoltClientOptions WithAllowPartialMessages( bool allowPartialMessages )
	{
		AllowPartialMessages = allowPartialMessages;
		return this;
	}

	public NetBoltClientOptions WithGlue( INetBoltGlue glue )
	{
		Glue = glue;
		return this;
	}

	public NetBoltClientOptions WithLogger( ILogger logger )
	{
		Logger = logger;
		return this;
	}

	public NetBoltClientOptions WithMaxMessageSize( int maxMessageSize )
	{
		MaxMessageSize = maxMessageSize;
		return this;
	}

	public NetBoltClientOptions WithMaxPartialMessageCount( int maxPartialMessageCount )
	{
		MaxPartialMessageCount = maxPartialMessageCount;
		return this;
	}

	public NetBoltClientOptions WithNetworkMessageCharacterEncoding( Encoding networkMessageCharacterEncoding )
	{
		NetworkMessageCharacterEncoding = networkMessageCharacterEncoding;
		return this;
	}

	public void Validate()
	{
		// Logger.
		ArgumentNullException.ThrowIfNull( Logger, nameof( Logger ) );

		// Max message size.
		if ( MaxMessageSize <= 0 )
			throw new ArgumentOutOfRangeException( nameof( MaxMessageSize ), $"{nameof( MaxMessageSize )} must be > 0" );

		// Max partial message count.
		if ( AllowPartialMessages && MaxPartialMessageCount <= 1 )
			throw new ArgumentOutOfRangeException( nameof( MaxPartialMessageCount ), $"{nameof( MaxPartialMessageCount )} must be > 1" );

		// Network message character encoding.
		ArgumentNullException.ThrowIfNull( NetworkMessageCharacterEncoding, nameof( NetworkMessageCharacterEncoding ) );
	}
}

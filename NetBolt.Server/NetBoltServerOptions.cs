using NetBolt.Glue;
using NetBolt.Glue.Logging;
using NetBolt.Shared;
using System;
using System.Net;
using System.Text;

namespace NetBolt.Server;

public sealed class NetBoltServerOptions
{
	public static NetBoltServerOptions Default => new();

	public bool AllowPartialMessages { get; set; }
	public INetBoltGlue? Glue { get; set; }
	public IPEndPoint IPEndPoint { get; set; } = new IPEndPoint( IPAddress.Any, 8080 );
	public ILogger Logger { get; set; } = NullLogger.Instance;
	public int MaxClients { get; set; } = 100;
	public int MaxClientsPerWriteThread { get; set; } = 25;
	public int MaxMessageSize { get; set; } = 65536;
	public int MaxPartialMessageCount { get; set; } = 10;
	public Encoding NetworkMessageCharacterEncoding { get; set; } = Encoding.Default;
	public TimeSpan PingTimeout { get; set; } = TimeSpan.FromSeconds( 5 );
	public bool StringCacheEnabled { get; set; } = true;

	public ExtensionContainer<IExtension> Extensions { get; } = new();

	public NetBoltServerOptions()
	{
	}

	public NetBoltServerOptions( NetBoltServerOptions options )
	{
		AllowPartialMessages = options.AllowPartialMessages;
		Glue = options.Glue;
		IPEndPoint = options.IPEndPoint;
		Logger = options.Logger;
		MaxClients = options.MaxClients;
		MaxClientsPerWriteThread = options.MaxClientsPerWriteThread;
		MaxMessageSize = options.MaxMessageSize;
		MaxPartialMessageCount = options.MaxPartialMessageCount;
		NetworkMessageCharacterEncoding = options.NetworkMessageCharacterEncoding;
		PingTimeout = options.PingTimeout;
		StringCacheEnabled = options.StringCacheEnabled;
	}

	public NetBoltServerOptions WithAllowPartialMessages( bool allowPartialMessages )
	{
		AllowPartialMessages = allowPartialMessages;
		return this;
	}

	public NetBoltServerOptions WithGlue( INetBoltGlue glue )
	{
		Glue = glue;
		return this;
	}

	public NetBoltServerOptions WithIPEndPoint( IPEndPoint ipEndPoint )
	{
		IPEndPoint = ipEndPoint;
		return this;
	}

	public NetBoltServerOptions WithLogger( ILogger logger )
	{
		Logger = logger;
		return this;
	}

	public NetBoltServerOptions WithMaxClients( int maxClients )
	{
		MaxClients = maxClients;
		return this;
	}

	public NetBoltServerOptions WithMaxClientsPerWriteThread( int maxClientsPerWriteThread )
	{
		MaxClientsPerWriteThread = maxClientsPerWriteThread;
		return this;
	}

	public NetBoltServerOptions WithMaxMessageSize( int maxMessageSize )
	{
		MaxMessageSize = maxMessageSize;
		return this;
	}

	public NetBoltServerOptions WithMaxPartialMessageCount( int maxPartialMessageCount )
	{
		MaxPartialMessageCount = maxPartialMessageCount;
		return this;
	}

	public NetBoltServerOptions WithNetworkMessageCharacterEncoder( Encoding networkMessageCharacterEncoding )
	{
		NetworkMessageCharacterEncoding = networkMessageCharacterEncoding;
		return this;
	}

	public NetBoltServerOptions WithPingTimeout( in TimeSpan pingTimeout )
	{
		PingTimeout = pingTimeout;
		return this;
	}

	public NetBoltServerOptions WithStringCacheEnabled( bool stringCacheEnabled )
	{
		StringCacheEnabled = stringCacheEnabled;
		return this;
	}

	public void Validate()
	{
		// IP end point.
		ArgumentNullException.ThrowIfNull( IPEndPoint, nameof( IPEndPoint ) );

		// Logger.
		ArgumentNullException.ThrowIfNull( Logger, nameof( Logger ) );

		// Max clients.
		if ( MaxClients <= 0 )
			throw new ArgumentOutOfRangeException( nameof( MaxClients ), $"{nameof( MaxClients )} must be > 0" );

		// Max clients per write thread.
		if ( MaxClientsPerWriteThread <= 0 )
			throw new ArgumentOutOfRangeException( nameof( MaxClientsPerWriteThread ), $"{nameof( MaxClientsPerWriteThread )} must be > 0" );

		// Max message size.
		if ( MaxMessageSize <= 0 )
			throw new ArgumentOutOfRangeException( nameof( MaxMessageSize ), $"{nameof( MaxMessageSize )} must be > 0" );

		// Max partial message count.
		if ( AllowPartialMessages && MaxPartialMessageCount <= 1 )
			throw new ArgumentOutOfRangeException( nameof( MaxPartialMessageCount ), $"{nameof( MaxPartialMessageCount )} must be > 1" );

		// Network message character encoding.
		ArgumentNullException.ThrowIfNull( NetworkMessageCharacterEncoding, nameof( NetworkMessageCharacterEncoding ) );

		// Ping timeout.
		if ( PingTimeout <= TimeSpan.Zero )
			throw new ArgumentOutOfRangeException( nameof( PingTimeout ), $"{nameof( PingTimeout )} must be > 0" );
	}
}

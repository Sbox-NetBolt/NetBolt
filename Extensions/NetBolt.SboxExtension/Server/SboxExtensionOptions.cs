using System;

namespace NetBolt.Extensions.Sbox;

public sealed class SboxExtensionOptions
{
	public static SboxExtensionOptions Default => new();

	public TimeSpan TokenCheckInterval { get; set; } = TimeSpan.FromMinutes( 1 );
	public TimeSpan TokenTimeout { get; set; } = TimeSpan.FromMinutes( 2 );

	public SboxExtensionOptions()
	{
	}

	public SboxExtensionOptions( SboxExtensionOptions other )
	{
		TokenCheckInterval = other.TokenCheckInterval;
		TokenTimeout = other.TokenTimeout;
	}

	public SboxExtensionOptions WithTokenCheckInterval( in TimeSpan tokenCheckInterval )
	{
		TokenCheckInterval = tokenCheckInterval;
		return this;
	}

	public SboxExtensionOptions WithTokenTimeout( in TimeSpan tokenTimeout )
	{
		TokenTimeout = tokenTimeout;
		return this;
	}

	public void Validate()
	{
		// Token check interval.
		if ( TokenCheckInterval <= TimeSpan.Zero )
			throw new ArgumentOutOfRangeException( nameof( TokenCheckInterval ), $"{nameof( TokenCheckInterval )} must be > 0" );

		if ( TokenCheckInterval >= TokenTimeout )
			throw new ArgumentOutOfRangeException( nameof( TokenCheckInterval ), $"{nameof( TokenCheckInterval )} cannot be >= {nameof( TokenTimeout )}" );

		// Token timeout.
		if ( TokenTimeout <= TimeSpan.Zero )
			throw new ArgumentOutOfRangeException( nameof( TokenTimeout ), $"{nameof( TokenTimeout )} must be > 0" );

		if ( TokenTimeout < TokenCheckInterval )
			throw new ArgumentOutOfRangeException( nameof( TokenTimeout ), $"{nameof( TokenTimeout )} cannot be < {nameof( TokenCheckInterval )}" );
	}
}

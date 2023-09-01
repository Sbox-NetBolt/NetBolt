using System;
using System.Diagnostics.CodeAnalysis;

namespace NetBolt;

public readonly struct ClientIdentifier : IEquatable<ClientIdentifier>
{
	public Platform Platform { get; }
	public long Identifier { get; }

	public ClientIdentifier( Platform platform, long identifier )
	{
		Platform = platform;
		Identifier = identifier;
	}

	public override bool Equals( object? obj )
	{
		return obj is ClientIdentifier other && Equals( other );
	}

	public bool Equals( ClientIdentifier other )
	{
		return Platform == other.Platform && Identifier == other.Identifier;
	}

	public override int GetHashCode()
	{
		return HashCode.Combine( Platform, Identifier );
	}

	public override string ToString()
	{
		return $"{Platform}: {Identifier}";
	}

	public static bool operator ==( in ClientIdentifier left, in ClientIdentifier right ) => left.Equals( right );
	public static bool operator !=( in ClientIdentifier left, in ClientIdentifier right ) => !(left == right);

	public static bool TryParse( string input, [NotNullWhen( true )] out ClientIdentifier? parsedIdentifier )
	{
		parsedIdentifier = null;

		var parts = input.Split( ':', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries );
		if ( parts.Length != 2 )
			return false;

		if ( !Enum.TryParse<Platform>( parts[0], true, out var platform ) )
			return false;

		if ( !long.TryParse( parts[0], out var identifier ) )
			return false;

		parsedIdentifier = new ClientIdentifier( platform, identifier );
		return true;
	}

	public static ClientIdentifier Parse( string input )
	{
		if ( !TryParse( input, out var parsedIdentifier ) )
			throw new ArgumentException( $"Failed to parse \"{input}\"", nameof( input ) );

		return parsedIdentifier.Value;
	}

	public static ClientIdentifier FromGeneric( long genericId ) => new( Platform.Generic, genericId );
	public static ClientIdentifier FromSteamId64( long steamId ) => new( Platform.Steam, steamId );
}

using NetBolt.Messaging;
using NetBolt.NetworkableExtension;
using System;

namespace NetBolt.Networkables;

public sealed class NetInt : NetVar<int>, IEquatable<NetInt>
{
	public NetInt() : base( default )
	{
	}

	public NetInt( int value ) : base( value )
	{
	}

	public override void Serialize( NetworkMessageWriter writer )
	{
		writer.Write( Value );
	}

	public override void Deserialize( NetworkMessageReader reader )
	{
		Value = reader.ReadInt32();
	}

	public override bool Equals( object? obj )
	{
		if ( obj is NetInt other )
			return Equals( other );
		else if ( obj is int number )
			return Equals( number );

		return false;
	}

	public bool Equals( NetInt? other )
	{
		return other is not null && Value == other.Value;
	}

	public override int GetHashCode() => Value.GetHashCode();
	public override string ToString() => Value.ToString();

	public static bool operator ==( NetInt left, NetInt right ) => left.Equals( right );
	public static bool operator !=( NetInt left, NetInt right ) => !(left == right);

	public static explicit operator int( NetInt netInt ) => netInt.Value;
	public static explicit operator NetInt( int @int ) => new( @int );
}

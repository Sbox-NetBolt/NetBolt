using NetBolt.Messaging;
using NetBolt.NetworkableExtension;
using System;

namespace NetBolt.Networkables;

public sealed class NetFloat : NetVar<float>, IEquatable<NetFloat>
{
	public NetFloat() : base( default )
	{
	}

	public NetFloat( float value ) : base( value )
	{
	}

	public override void Serialize( NetworkMessageWriter writer )
	{
		writer.Write( Value );
	}

	public override void Deserialize( NetworkMessageReader reader )
	{
		Value = reader.ReadSingle();
	}

	public override bool Equals( object? obj )
	{
		if ( obj is NetFloat other )
			return Equals( other );
		else if ( obj is float number )
			return Equals( number );

		return false;
	}

	public bool Equals( NetFloat? other )
	{
		return other is not null && Value == other.Value;
	}

	public override int GetHashCode() => Value.GetHashCode();
	public override string ToString() => Value.ToString();

	public static bool operator ==( NetFloat left, NetFloat right ) => left.Equals( right );
	public static bool operator !=( NetFloat left, NetFloat right ) => !(left == right);

	public static explicit operator float( NetFloat netFloat ) => netFloat.Value;
	public static explicit operator NetFloat( float @float ) => new( @float );
}

using NetBolt.Messaging;
using NetBolt.NetworkableExtension;
using System;

namespace NetBolt.Networkables;

public sealed class NetByte : NetVar<byte>, IEquatable<NetByte>
{
	public NetByte() : base( default )
	{
	}

	public NetByte( byte value ) : base( value )
	{
	}

	public override void Serialize( NetworkMessageWriter writer )
	{
		writer.Write( Value );
	}

	public override void Deserialize( NetworkMessageReader reader )
	{
		Value = reader.ReadByte();
	}

	public override bool Equals( object? obj )
	{
		if ( obj is NetByte other )
			return Equals( other );
		else if ( obj is byte number )
			return Equals( number );

		return false;
	}

	public bool Equals( NetByte? other )
	{
		return other is not null && Value == other.Value;
	}

	public override int GetHashCode() => Value.GetHashCode();
	public override string ToString() => Value.ToString();

	public static bool operator ==( NetByte left, NetByte right ) => left.Equals( right );
	public static bool operator !=( NetByte left, NetByte right ) => !(left == right);

	public static explicit operator byte( NetByte netByte ) => netByte.Value;
	public static explicit operator NetByte( byte @byte ) => new( @byte );
}

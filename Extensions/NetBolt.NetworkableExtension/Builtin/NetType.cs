using NetBolt.Messaging;
using NetBolt.NetworkableExtension;
using System;

namespace NetBolt.Networkables;

public sealed class NetType : NetVar<Type>, IEquatable<NetType>
{
	public NetType() : base( typeof( object ) )
	{
	}

	public NetType( Type value ) : base( value )
	{
	}

	public override void Serialize( NetworkMessageWriter writer )
	{
		writer.Write( Value );
	}

	public override void Deserialize( NetworkMessageReader reader )
	{
		Value = reader.ReadType();
	}

	public override bool Equals( object? obj )
	{
		if ( obj is NetType other )
			return Equals( other );
		else if ( obj is Type str )
			return Equals( str );

		return false;
	}

	public bool Equals( NetType? other )
	{
		return other is not null && Value == other.Value;
	}

	public override int GetHashCode() => Value.GetHashCode();
	public override string ToString() => Value.ToString();

	public static bool operator ==( NetType left, NetType right ) => left.Equals( right );
	public static bool operator !=( NetType left, NetType right ) => !(left == right);

	public static explicit operator Type( NetType netType ) => netType.Value;
	public static explicit operator NetType( Type type ) => new( type );
}

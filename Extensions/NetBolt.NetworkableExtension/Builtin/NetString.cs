using NetBolt.Messaging;
using NetBolt.NetworkableExtension;
using System;

namespace NetBolt.Networkables;

public sealed class NetString : NetVar<string>, IEquatable<NetString>
{
	public int Length => Value.Length;
	public char this[int index] => Value[index];

	public NetString() : base( string.Empty )
	{
	}

	public NetString( string value ) : base( value )
	{
	}

	public override void Serialize( NetworkMessageWriter writer )
	{
		writer.Write( Value );
	}

	public override void Deserialize( NetworkMessageReader reader )
	{
		Value = reader.ReadString();
	}

	public override bool Equals( object? obj )
	{
		if ( obj is NetString other )
			return Equals( other );
		else if ( obj is string str )
			return Equals( str );

		return false;
	}

	public bool Equals( NetString? other )
	{
		return other is not null && Value == other.Value;
	}

	public override int GetHashCode() => Value.GetHashCode();
	public override string ToString() => Value;

	public static bool operator ==( NetString left, NetString right ) => left.Equals( right );
	public static bool operator !=( NetString left, NetString right ) => !(left == right);

	public static explicit operator string( NetString netString ) => netString.Value;
	public static explicit operator NetString( string str ) => new( str );
}

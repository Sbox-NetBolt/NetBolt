using NetBolt.Messaging;
using System;

namespace NetBolt.NetworkableExtension;

public abstract class NetVar<T> : INetworkable, IEquatable<T> where T : notnull
{
	public T Value
	{
		get => value;
		set
		{
			ArgumentNullException.ThrowIfNull( value, nameof( value ) );
			this.value = value;
		}
	}
	private T value;

	protected NetVar( T value )
	{
		ArgumentNullException.ThrowIfNull( value, nameof( value ) );
		this.value = value;
	}

	public abstract void Serialize( NetworkMessageWriter writer );
	public abstract void Deserialize( NetworkMessageReader reader );

	public virtual bool Equals( T? other )
	{
		if ( Value is IEquatable<T> equatable )
			return equatable.Equals( other );

		if ( other is IEquatable<T> otherEquatable )
			return otherEquatable.Equals( Value );

		return ReferenceEquals( Value, other );
	}
}

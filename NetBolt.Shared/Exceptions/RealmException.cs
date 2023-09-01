using NetBolt.Glue;
using System;
using System.Diagnostics;

namespace NetBolt.Exceptions;

public sealed class RealmException : Exception
{
	public RealmException()
	{
	}

	public RealmException( string? message ) : base( message )
	{
	}

	public RealmException( string? message, Exception? innerException ) : base( message, innerException )
	{
	}

	[StackTraceHidden]
	public static void ThrowIf( INetBoltGlue glue, Realm realm )
	{
		if ( glue.Realm == realm )
			throw new RealmException( $"This operation cannot be completed within the \"{realm}\" realm" );
	}

	[StackTraceHidden]
	public static void ThrowIfNot( INetBoltGlue glue, Realm realm )
	{
		if ( glue.Realm != realm )
			throw new RealmException( $"This operation cannot be completed outside of the \"{realm}\" realm" );
	}
}

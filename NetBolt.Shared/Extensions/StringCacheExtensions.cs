using System;

namespace NetBolt.Shared.Extensions;

public static class StringCacheExtensions
{
	public static uint Add( this StringCache cache, Type type )
	{
		ArgumentNullException.ThrowIfNull( type, nameof( type ) );
		return cache.Add( type.FullName ?? type.Name );
	}

	public static void Remove( this StringCache cache, Type type )
	{
		ArgumentNullException.ThrowIfNull( type, nameof( type ) );
		cache.Remove( type.FullName ?? type.Name );
	}
}

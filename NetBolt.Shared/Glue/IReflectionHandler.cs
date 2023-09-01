using System;
using System.Collections.Generic;

namespace NetBolt.Glue;

public interface IReflectionHandler
{
	object? CreateInstance( Type type );
	Type? GetType( string typeName );
	IEnumerable<Type> GetTypesAssignableTo( Type type );
	IEnumerable<Type> GetTypesAssignableTo<T>() => GetTypesAssignableTo( typeof( T ) );
}

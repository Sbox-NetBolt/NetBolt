using NetBolt.Shared;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace NetBolt.Server;

public sealed class ExtensionContainer<TExtensionBase> : IEnumerable<TExtensionBase> where TExtensionBase : IExtension
{
	public ImmutableArray<TExtensionBase> Extensions { get; private set; } = ImmutableArray<TExtensionBase>.Empty;

	public TExtension AddExtension<TExtension>() where TExtension : TExtensionBase, new()
	{
		if ( HasExtension<TExtension>() )
			throw new ArgumentException( $"An instance of {typeof( TExtension )} is already contained" );

		var newExtension = new TExtension();
		Extensions = Extensions.Add( newExtension );
		return newExtension;
	}

	public void AddExtension<TExtension>( TExtension extension ) where TExtension : TExtensionBase
	{
		if ( HasExtension<TExtension>() )
			throw new ArgumentException( $"An instance of {extension.GetType()} is already contained", nameof( extension ) );

		Extensions = Extensions.Add( extension );
	}

	public bool HasExtension<TExtension>() where TExtension : TExtensionBase => HasExtension( typeof( TExtension ) );
	public bool HasExtension( Type type )
	{
		if ( !type.IsAssignableTo( typeof( TExtensionBase ) ) )
			throw new ArgumentException( $"The type {type} is not assignable to {typeof( TExtensionBase )}", nameof( type ) );

		foreach ( var extension in Extensions )
		{
			if ( extension.GetType() == type )
				return true;
		}

		return false;
	}

	public bool TryGetExtension<TExtension>( [NotNullWhen( true )] out TExtension? extension ) where TExtension : TExtensionBase
	{
		foreach ( var containedExtension in Extensions )
		{
			if ( containedExtension is not TExtension )
				continue;

			extension = (TExtension)containedExtension;
			return true;
		}

		extension = default;
		return false;
	}

	public IEnumerator<TExtensionBase> GetEnumerator()
	{
		foreach ( var extension in Extensions )
			yield return extension;
	}

	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

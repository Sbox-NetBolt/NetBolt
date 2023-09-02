using NetBolt.Shared;
using System;
using Xunit;

namespace NetBolt.Tests.Shared;

public sealed class ExtensionContainer
{
	#region AddExtension
	#region AddExtension<T>
	[Fact]
	public void AddGenericExtensionToContainer()
	{
		// Given:
		var container = new ExtensionContainer<IExtension>();

		// When:
		container.AddExtension<TestExtension>();

		// Then:
		var extension = Assert.Single( container.Extensions );
		Assert.Equal( TestExtension.DefaultExtensionName, extension.Name );
	}

	[Fact]
	public void AddGenericExtensionThrowsOnDuplicateType()
	{
		// Given:
		const string addExtensionGenericParameterName = "TExtension";
		var container = new ExtensionContainer<IExtension>();
		container.AddExtension<TestExtension>();

		// When:
		void Execute()
		{
			container.AddExtension<TestExtension>();
		}

		// Then:
		Assert.Throws<ArgumentException>( addExtensionGenericParameterName, Execute );
	}

	[Fact]
	public void AddGenericDerivedExtensionDoesNotThrowWithDuplicateType()
	{
		// Given:
		var container = new ExtensionContainer<IExtension>();
		container.AddExtension<TestExtension>();

		// When:
		container.AddExtension<DerivedExtension>();

		// Then:
		Assert.Equal( 2, container.Extensions.Length );
		Assert.True( container.Extensions[0] is TestExtension );
		Assert.True( container.Extensions[1] is DerivedExtension );
	}
	#endregion

	#region AddExtension<TExtension>( TExtension )
	[Fact]
	public void AddExtensionToContainer()
	{
		// Given:
		var container = new ExtensionContainer<IExtension>();
		IExtension extension = new TestExtension();

		// When:
		container.AddExtension( extension );

		// Then:
		extension = Assert.Single( container.Extensions );
		Assert.Equal( TestExtension.DefaultExtensionName, extension.Name );
	}

	[Fact]
	public void AddExtensionThrowsOnDuplicateType()
	{
		// Given:
		const string addExtensionParameterName = "extension";
		var container = new ExtensionContainer<IExtension>();
		container.AddExtension( new TestExtension() );

		// When:
		void Execute()
		{
			container.AddExtension( new TestExtension() );
		}

		// Then:
		Assert.Throws<ArgumentException>( addExtensionParameterName, Execute );
	}

	[Fact]
	public void AddDerivedExtensionDoesNotThrowWithDuplicateType()
	{
		// Given:
		var container = new ExtensionContainer<IExtension>();
		container.AddExtension( new TestExtension() );

		// When:
		container.AddExtension( new DerivedExtension() );

		// Then:
		Assert.Equal( 2, container.Extensions.Length );
		Assert.True( container.Extensions[0] is TestExtension );
		Assert.True( container.Extensions[1] is DerivedExtension );
	}
	#endregion
	#endregion

	#region HasExtension
	#region HasExtension<T>
	[Fact]
	public void HasExtensionGeneric()
	{
		// Given:
		var container = new ExtensionContainer<IExtension>();
		container.AddExtension<TestExtension>();

		// When:
		var hasExtension = container.HasExtension<TestExtension>();

		// Then:
		Assert.True( hasExtension );
	}

	[Fact]
	public void HasExtensionGenericFail()
	{
		// Given:
		var container = new ExtensionContainer<IExtension>();

		// When:
		var hasExtension = container.HasExtension<TestExtension>();

		// Then:
		Assert.False( hasExtension );
	}

	[Fact]
	public void HasExtensionGenericFailsOnBaseTypeWithoutFuzzy()
	{
		// Given:
		var container = new ExtensionContainer<IExtension>();
		container.AddExtension<DerivedExtension>();

		// When:
		var hasExtension = container.HasExtension<TestExtension>();

		// Then:
		Assert.False( hasExtension );
	}
	#endregion
	
	#region HasExtension( Type )
	[Fact]
	public void HasExtension()
	{
		// Given:
		var container = new ExtensionContainer<IExtension>();
		container.AddExtension<TestExtension>();

		// When:
		var hasExtension = container.HasExtension( typeof( TestExtension ) );

		// Then:
		Assert.True( hasExtension );
	}

	[Fact]
	public void HasExtensionFail()
	{
		// Given:
		var container = new ExtensionContainer<IExtension>();

		// When:
		var hasExtension = container.HasExtension( typeof( TestExtension ) );

		// Then:
		Assert.False( hasExtension );
	}

	[Fact]
	public void HasExtensionThrowsOnNotApplicableType()
	{
		// Given:
		const string hasExtensionTypeParameterName = "type";
		var container = new ExtensionContainer<IExtension>();

		// When:
		void Execute()
		{
			container.HasExtension( typeof( object ) );
		}

		// Then:
		Assert.Throws<ArgumentException>( hasExtensionTypeParameterName, Execute );
	}

	[Fact]
	public void HasExtensionFailsOnBaseTypeWithoutFuzzy()
	{
		// Given:
		var container = new ExtensionContainer<IExtension>();
		container.AddExtension<DerivedExtension>();

		// When:
		var hasExtension = container.HasExtension( typeof( TestExtension ) );

		// Then:
		Assert.False( hasExtension );
	}
	#endregion
	#endregion

	#region TryGetExtension
	[Fact]
	public void TryGetExtensionFromContainer()
	{
		// Given:
		var container = new ExtensionContainer<IExtension>();
		container.AddExtension<TestExtension>();

		// When:
		var hasExtension = container.TryGetExtension<TestExtension>( out var extension );

		// Then:
		Assert.True( hasExtension );
		Assert.True( extension is TestExtension );
	}

	[Fact]
	public void TryGetExtensionFromContainerFail()
	{
		// Given:
		var container = new ExtensionContainer<IExtension>();

		// When:
		var hasExtension = container.TryGetExtension<TestExtension>( out var extension );

		// Then:
		Assert.False( hasExtension );
		Assert.Null( extension );
	}

	[Fact]
	public void TryGetExtensionFailsOnBaseTypeWithoutFuzzy()
	{
		// Given:
		var container = new ExtensionContainer<IExtension>();
		container.AddExtension<DerivedExtension>();

		// When:
		var hasExtension = container.TryGetExtension<TestExtension>( out var extension );

		// Then:
		Assert.False( hasExtension );
		Assert.Null( extension );
	}
	#endregion
}

file class TestExtension : IExtension
{
	internal const string DefaultExtensionName = "Test Extension";

	public string Name { get; }

	public TestExtension() : this( DefaultExtensionName )
	{
	}

	public TestExtension( string extensionName )
	{
		Name = extensionName;
	}

	public void Start()
	{
	}

	public void Stop()
	{
	}

	public void ProcessEvents()
	{
	}
}

file class DerivedExtension : TestExtension
{
}

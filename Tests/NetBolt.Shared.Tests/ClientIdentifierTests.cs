﻿using Xunit;

namespace NetBolt.Tests.Shared;

public sealed class ClientIdentifierTests
{
	[InlineData( true, Platform.Generic, 1234, Platform.Generic, 1234 )]
	[InlineData( false, Platform.Generic, 1234, Platform.Steam, 1234 )]
	[InlineData( false, Platform.Generic, 12345, Platform.Generic, 4321 )]
	[Theory]
	public void Equal( bool isEqual, Platform leftPlatform, long leftIdentifier, Platform rightPlatform, long rightIdentifier )
	{
		var leftClientIdent = new ClientIdentifier( leftPlatform, leftIdentifier );
		var rightClientIdent = new ClientIdentifier( rightPlatform, rightIdentifier );

		Assert.Equal( isEqual, leftClientIdent == rightClientIdent );
	}

	[InlineData( "generic:1234" )]
	[InlineData( "gEnErIC:1234" )]
	[Theory]
	public void TryParse( string input )
	{
		Assert.True( ClientIdentifier.TryParse( input, out var parsedIdentifier ) );
		Assert.Equal( Platform.Generic, parsedIdentifier.Value.Platform );
		Assert.Equal( 1234, parsedIdentifier.Value.Identifier );
	}

	[Fact]
	public void TryParseBadPlatform()
	{
		Assert.False( ClientIdentifier.TryParse( "unknown:1234", out var parsedIdentifier ) );
		Assert.Null( parsedIdentifier );
	}

	[Fact]
	public void TryParseBadIdentifier()
	{
		Assert.False( ClientIdentifier.TryParse( "generic:abc", out var parsedIdentifier ) );
		Assert.Null( parsedIdentifier );
	}
}

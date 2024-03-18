using NetBolt.Messaging.Messages;
using NetBolt.Tests.Shared.Mocks;
using System;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;

namespace NetBolt.Tests.Shared;

public class PartialMessageTests
{
	[Fact]
	public void CreateFrom()
	{
		// Given:
		const int maxMessageSize = 100;

		using var stream = TestUtility.CreateStream( testDataWriter =>
		{
			testDataWriter.Write( new byte[100] );
		} );
		var messageBytes = ((MemoryStream)stream).ToArray();

		// When:
		var messages = PartialMessage.CreateFrom( new ServerGlue(), messageBytes, maxMessageSize, Encoding.Default ).ToArray();

		// Then:
		Assert.Equal( 2, messages.Length );
		for ( var i = 0; i < messages.Length; i++ )
		{
			Assert.Equal( 2, messages[i].NumPieces );
			Assert.Equal( 50, messages[i].PartialData.Count );
		}
	}

	[Fact]
	public void CreateFromThrowsOnSmallMaxMessageSize()
	{
		// Given:
		const string createFromParameterName = "maxMessageSize";

		// When:
		static void Execute()
		{
			var message = PartialMessage.CreateFrom( new ServerGlue(), Array.Empty<byte>(), 0, Encoding.Default ).First();
		}

		// Then:
		Assert.Throws<ArgumentException>( createFromParameterName, Execute );
	}

	[Fact]
	public void CreateFromThrowsOnNullGlue()
	{
		// Given:
		const string createFromParameterName = "glue";

		// When:
		static void Execute()
		{
			_ = PartialMessage.CreateFrom( null!, Array.Empty<byte>(), 0, Encoding.Default ).First();
		}

		// Then:
		Assert.Throws<ArgumentNullException>( createFromParameterName, Execute );
	}

	[Fact]
	public void CreateFromThrowsOnNullMessageBytes()
	{
		// Given:
		const string createFromParameterName = "messageBytes";

		// When:
		static void Execute()
		{
			_ = PartialMessage.CreateFrom( new ServerGlue(), null!, 0, Encoding.Default ).First();
		}

		// Then:
		Assert.Throws<ArgumentNullException>( createFromParameterName, Execute );
	}

	[Fact]
	public void CreateFromThrowsOnNullEncoding()
	{
		// Given:
		const string createFromParameterName = "encoding";

		// When:
		static void Execute()
		{
			_ = PartialMessage.CreateFrom( new ServerGlue(), Array.Empty<byte>(), int.MaxValue, null! ).First();
		}

		// Then:
		Assert.Throws<ArgumentNullException>( createFromParameterName, Execute );
	}
}

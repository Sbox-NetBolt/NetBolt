using System;
using System.IO;
using System.Text;

namespace NetBolt.Tests.Shared;

internal static class TestUtility
{
	internal static Stream CreateStream( Action<BinaryWriter> writeCb )
	{
		var stream = new MemoryStream();
		using ( var writer = new BinaryWriter( stream, Encoding.Default, true ) )
		{
			writeCb( writer );
			stream.Position = 0;
		}

		return stream;
	}
}

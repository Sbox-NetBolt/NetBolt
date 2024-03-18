using System;

namespace NetBolt.Glue.Logging;

public interface ILogger
{
	bool IsEnabled( LoggerLevel level );

	void Error( string message, Exception? exception = null );
	void Warning( string message, Exception? exception = null );
	void Information( string message );
	void Debug( string message, Exception? exception = null );
}

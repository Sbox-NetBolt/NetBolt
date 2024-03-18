using System;

namespace NetBolt.Glue.Logging;

public sealed class NullLogger : ILogger
{
	public static NullLogger Instance { get; } = new NullLogger();

	/// <inheritdoc/>
	public bool IsEnabled( LoggerLevel level ) => false;

	/// <inheritdoc/>
	public void Error( string message, Exception? exception = null )
	{
	}

	/// <inheritdoc/>
	public void Warning( string message, Exception? exception = null )
	{
	}

	/// <inheritdoc/>
	public void Information( string message )
	{
	}

	/// <inheritdoc/>
	public void Debug( string message, Exception? exception = null )
	{
	}
}

using NetBolt.Glue;
using NetBolt.Glue.Logging;
using NetBolt.Messaging;
using NetBolt.Messaging.Messages;
using Sandbox;
using Sandbox.Services;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace NetBolt.Client;

public sealed class NetBoltClient : IAsyncDisposable, IDisposable
{
	public bool Connected => !disconnected;
	public ServerDisconnectReason? DisconnectReason { get; private set; }

	public delegate void ConnectedHandler( NetBoltClient socket );
	public delegate void DisconnectedHandler( NetBoltClient socket, ServerDisconnectReason reason );
	public delegate void ServerMessageHandler( NetBoltClient socket, NetworkMessage message );

	public event ConnectedHandler? OnConnected;
	public event DisconnectedHandler? OnDisconnected;
	public event ServerMessageHandler? OnServerMessageReceived;

	private Task? writeTask;
	private bool closeRequested;
	private bool disconnected = true;
	private bool disposed;
	private int partialReceivedCount;

	private readonly NetBoltClientOptions options;
	private readonly INetBoltGlue glue;
	private readonly int absoluteMaxMessageSize;
	private readonly bool allowPartialMessages;
	private readonly ILogger logger;
	private readonly int maxMessageSize;
	private readonly int maxPartialMessageCount;
	private readonly Encoding networkMessageCharacterEncoding;
	private readonly WebSocket socket;
	private readonly ConcurrentQueue<NetworkMessage> pendingIncomingMessages = new();
	private readonly ConcurrentQueue<NetworkMessage> pendingOutgoingMessages = new();
	private readonly byte[] incomingPartialMessageBuffer;
	private readonly byte[] outgoingPartialMessageBuffer;
	private readonly byte[] writeBuffer;

	public NetBoltClient() : this( NetBoltClientOptions.Default )
	{
	}

	public NetBoltClient( NetBoltClientOptions options )
	{
		options.Validate();
		this.options = new NetBoltClientOptions( options );

		if ( options.Glue is not null )
			glue = options.Glue;
		else
			glue = new DefaultGlue( options.Logger );

		allowPartialMessages = options.AllowPartialMessages;
		logger = options.Logger;
		maxMessageSize = options.MaxMessageSize;
		maxPartialMessageCount = options.MaxPartialMessageCount;
		networkMessageCharacterEncoding = options.NetworkMessageCharacterEncoding;
		absoluteMaxMessageSize = maxMessageSize * (allowPartialMessages ? maxPartialMessageCount : 1);

		socket = new WebSocket( maxMessageSize );
		socket.OnDataReceived += OnDataReceived;
		socket.OnDisconnected += OnSocketDisconnected;

		incomingPartialMessageBuffer = new byte[absoluteMaxMessageSize];
		outgoingPartialMessageBuffer = new byte[absoluteMaxMessageSize];
		writeBuffer = new byte[absoluteMaxMessageSize];
	}

	public void Dispose()
	{
		Dispose( disposing: true );
		GC.SuppressFinalize( this );
	}

	public async ValueTask DisposeAsync()
	{
		if ( Connected )
			await DisconnectAsync();

		Dispose( disposing: true );
		GC.SuppressFinalize( this );
	}

	private void Dispose( bool disposing )
	{
		if ( disposed )
			return;

		if ( disposing )
			socket.Dispose();

		disposed = true;
	}

	public async Task ConnectAsync( string uri )
	{
		if ( disposed )
			throw new ObjectDisposedException( nameof( NetBoltClient ) );

		if ( Connected )
			throw new InvalidOperationException( "The socket is already connected to a server" );

		ValidateUri( uri );

		if ( logger.IsEnabled( LoggerLevel.Debug ) )
			logger.Debug( "Getting S&box auth token" );

		var token = await Auth.GetToken( "NetBolt" );
		var headers = new Dictionary<string, string>
		{
			{ "steamid", Game.SteamId.ToString() },
			{ "token", token }
		};

		if ( logger.IsEnabled( LoggerLevel.Debug ) )
			logger.Debug( $"Connecting to {uri}..." );

		disconnected = false;
		DisconnectReason = null;
		glue.StringCache.Swap( ImmutableArray<KeyValuePair<string, uint>>.Empty );
		await socket.Connect( uri, headers );
		PostConnect();
	}

	public async Task ConnectAsync( string uri, Dictionary<string, string> headers )
	{
		if ( disposed )
			throw new ObjectDisposedException( nameof( NetBoltClient ) );

		if ( Connected )
			throw new InvalidOperationException( "The socket is already connected to a server" );

		ValidateUri( uri );

		if ( logger.IsEnabled( LoggerLevel.Debug ) )
			logger.Debug( $"Connecting to {uri}..." );

		disconnected = false;
		DisconnectReason = null;
		glue.StringCache.Swap( ImmutableArray<KeyValuePair<string, uint>>.Empty );
		await socket.Connect( uri, headers );
		PostConnect();
	}

	private void PostConnect()
	{
		writeTask = GameTask.RunInThreadAsync( WriteThreadLoopAsync );

		if ( logger.IsEnabled( LoggerLevel.Information ) )
			logger.Information( "Connected to server" );

		OnConnected?.Invoke( this );
	}

	public async Task DisconnectAsync()
	{
		if ( disposed )
			throw new ObjectDisposedException( nameof( NetBoltClient ) );

		if ( !Connected )
			throw new InvalidOperationException( "The socket is not connected to a server" );

		if ( closeRequested )
			return;

		closeRequested = true;

		QueueMessage( new DisconnectMessage( ServerDisconnectReason.Requested ) );
		if ( writeTask is not null )
			await writeTask;

		while ( !disconnected )
		{
			await Task.Delay( 1 );
			ProcessIncomingMessages();
		}

		closeRequested = false;
		InternalOnDisconnected();
	}

	public void ProcessIncomingMessages()
	{
		if ( disposed )
			throw new ObjectDisposedException( nameof( NetBoltClient ) );

		while ( pendingIncomingMessages.TryDequeue( out var message ) )
		{
			if ( logger.IsEnabled( LoggerLevel.Debug ) )
				logger.Debug( $"Received {message} from server" );

			switch ( message )
			{
				case DisconnectMessage disconnectMessage:
					DisconnectReason = disconnectMessage.Reason;
					disconnected = true;

					if ( !closeRequested )
						InternalOnDisconnected();
					return;
				case StringCacheUpdateMessage stringCacheUpdateMessage:
					UpdateStringCache( stringCacheUpdateMessage );
					return;
				case SboxTokenRequestMessage:
					_ = SendNewTokenAsync();
					return;
			}

			OnServerMessageReceived?.Invoke( this, message );
		}
	}

	private void UpdateStringCache( StringCacheUpdateMessage stringCacheUpdateMessage )
	{
		glue.StringCache.Swap( stringCacheUpdateMessage.Entries );
	}

	private async Task SendNewTokenAsync()
	{
		var token = await Auth.GetToken( "NetBolt" );
		QueueMessage( new SboxTokenRequestMessage( token ) );
	}

	public void QueueMessage( NetworkMessage message )
	{
		if ( disposed )
			throw new ObjectDisposedException( nameof( NetBoltClient ) );

		ArgumentNullException.ThrowIfNull( message, nameof( message ) );

		pendingOutgoingMessages.Enqueue( message );

		if ( logger.IsEnabled( LoggerLevel.Debug ) )
			logger.Debug( $"Queueing {message} to send to server" );
	}

	private void OnDataReceived( Span<byte> data )
	{
		NetworkMessage message;
		try
		{
			message = NetworkMessage.Parse( glue, data.ToArray(), networkMessageCharacterEncoding );
		}
		catch ( Exception e )
		{
			if ( logger.IsEnabled( LoggerLevel.Error ) )
				logger.Error( $"An exception occurred during deserialization of a {nameof( NetworkMessage )}", e );

			return;
		}

		if ( message is PartialMessage partialMessage )
		{
			ReceivePartialMessage( partialMessage );
			return;
		}

		pendingIncomingMessages.Enqueue( message );

		if ( logger.IsEnabled( LoggerLevel.Debug ) )
			logger.Debug( $"Queueing {message} ({data.Length} bytes) from server" );

		if ( message is StringCacheUpdateMessage )
		{
			ProcessIncomingMessages();
			return;
		}
	}

	private void ReceivePartialMessage( PartialMessage partialMessage )
	{
		if ( !allowPartialMessages )
		{
			if ( logger.IsEnabled( LoggerLevel.Error ) )
				logger.Error( $"Received a partial message when they are not allowed" );

			return;
		}

		if ( partialMessage.NumPieces > maxPartialMessageCount )
		{
			if ( logger.IsEnabled( LoggerLevel.Error ) )
				logger.Error( $"Received a partial message that will contain more pieces than what is allowed ({maxPartialMessageCount} allowed, {partialMessage.NumPieces} expected to received)" );

			return;
		}

		partialMessage.PartialData.CopyTo( incomingPartialMessageBuffer, maxMessageSize * partialReceivedCount );
		partialReceivedCount++;

		if ( partialReceivedCount != partialMessage.NumPieces )
			return;

		var partialHeaderSize = NetworkMessage.GetHeaderSize<PartialMessage>( glue, networkMessageCharacterEncoding );
		var messageSize = (maxMessageSize - partialHeaderSize) * (partialReceivedCount - 1) + partialMessage.PartialData.Count;
		partialReceivedCount = 0;
		OnDataReceived( incomingPartialMessageBuffer.AsSpan( 0, messageSize ) );
	}

	private void OnSocketDisconnected( int status, string reason )
	{
		// Unexpected closure from server disappearing.
		if ( status != 1003 )
			return;

		DisconnectReason = ServerDisconnectReason.UnexpectedDisconnect;
		disconnected = true;
		InternalOnDisconnected();
	}

	private void InternalOnDisconnected()
	{
		socket.Dispose();

		DisconnectReason ??= ServerDisconnectReason.UnexpectedDisconnect;
		if ( logger.IsEnabled( LoggerLevel.Information ) )
			logger.Information( $"Disconnected from server for reason {DisconnectReason}" );

		OnDisconnected?.Invoke( this, DisconnectReason.Value );
	}

	private async Task WriteThreadLoopAsync()
	{
		while ( Connected && !closeRequested )
		{
			await WriteMessagesAsync();
			await Task.Delay( 1 );
		}

		await WriteMessagesAsync();
	}

	private async Task WriteMessagesAsync()
	{
		while ( pendingOutgoingMessages.TryDequeue( out var message ) )
		{
			using var stream = new MemoryStream( writeBuffer, true );
			try
			{
				NetworkMessage.WriteToStream( glue, stream, message, networkMessageCharacterEncoding );
			}
			catch ( Exception e )
			{
				if ( logger.IsEnabled( LoggerLevel.Error ) )
					logger.Error( $"An exception occurred during serialization of a {nameof( NetworkMessage )}", e );

				continue;
			}

			var messageSize = stream.Position;
			if ( messageSize > absoluteMaxMessageSize )
			{
				if ( logger.IsEnabled( LoggerLevel.Error ) )
					logger.Error( $"The message {message} ({messageSize} bytes) exceeds the maximum size that can be sent ({absoluteMaxMessageSize} bytes)" );

				continue;
			}

			if ( allowPartialMessages && messageSize > maxMessageSize )
			{
				if ( !await SendPartialMessagesAsync( messageSize ) )
					continue;
			}
			else
				await socket.Send( writeBuffer.AsSpan( 0, (int)messageSize ) );

			if ( logger.IsEnabled( LoggerLevel.Debug ) )
				logger.Debug( $"Sent {message} ({messageSize} bytes) to server" );
		}
	}

	private async ValueTask<bool> SendPartialMessagesAsync( long messageSize )
	{
		try
		{
			foreach ( var (partialMessage, partialMessageSize) in PartialMessage.CreateFrom( glue, writeBuffer, messageSize, maxMessageSize,
				networkMessageCharacterEncoding, outgoingPartialMessageBuffer ) )
			{
				await socket.Send( outgoingPartialMessageBuffer.AsSpan( 0, (int)partialMessageSize ) );
			}
		}
		catch ( Exception e )
		{
			if ( logger.IsEnabled( LoggerLevel.Error ) )
				logger.Error( $"An exception occurred during serialization of a {nameof( PartialMessage )}", e );

			return false;
		}

		return true;
	}

	private static void ValidateUri( string uri )
	{
		ArgumentException.ThrowIfNullOrEmpty( uri, nameof( uri ) );

		if ( !Uri.TryCreate( uri, UriKind.Absolute, out var result ) )
			throw new ArgumentException( "WebSocket URI is not a valid URI.", nameof( uri ) );

		if ( result.Scheme != "ws" && result.Scheme != "wss" )
			throw new ArgumentException( "WebSocket URI must use the ws:// or wss:// scheme.", nameof( uri ) );
	}
}

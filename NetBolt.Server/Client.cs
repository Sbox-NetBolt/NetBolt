using NetBolt.Glue.Logging;
using NetBolt.Messaging;
using NetBolt.Messaging.Messages;
using NetBolt.Server.Extensions;
using NetBolt.Server.Util;
using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;
using System;
using System.Text;
using System.Threading;
using vtortola.WebSockets;
using ILogger = NetBolt.Glue.Logging.ILogger;
using NetBolt.Glue;

namespace NetBolt.Server;

public sealed class Client : IClient, IDisposable
{
	internal delegate void MessageReceived( Client client, NetworkMessage message );
	internal static event MessageReceived? OnMessageReceived;

	public double Ping
	{
		get
		{
			if ( !Connected )
				return -1;

			return socket.Latency.TotalMilliseconds;
		}
	}
	public ClientIdentifier Identifier { get; }
	public NetBoltServerExtension ValidatingExtension { get; }

	public bool Connected => socket.IsConnected;
	public ServerDisconnectReason? DisconnectReason { get; internal set; } = null;

	internal WriteThread? WriteThread { private get; set; }

	private bool disposed;
	private TemporaryArrayAccess<byte> partialMessageBuffer;
	private int partialReceivedCount;

	private readonly INetBoltGlue glue;
	private readonly NetBoltServer owner;
	private readonly WebSocket socket;
	private readonly int absoluteMaxMessageSize;
	private readonly bool allowPartialMessages;
	private readonly ILogger logger;
	private readonly int maxMessageSize;
	private readonly int maxPartialMessageCount;
	private readonly Encoding networkMessageCharacterEncoding;
	private readonly CancellationTokenSource clientTokenSource = new();
	private readonly ConcurrentQueue<NetworkMessage> PendingIncomingMessages = new();
	private readonly ConcurrentQueue<NetworkMessage> PendingOutgoingMessages = new();

	private readonly SemaphoreSlim disconnectSemaphore = new( 1 );
	private readonly SemaphoreSlim writeSemaphore = new( 1 );

	internal Client( NetBoltServer owner, WebSocket socket )
	{
		glue = owner.Glue;
		this.owner = owner;
		this.socket = socket;
		allowPartialMessages = owner.Options.AllowPartialMessages;
		logger = owner.Options.Logger;
		maxMessageSize = owner.Options.MaxMessageSize;
		maxPartialMessageCount = owner.Options.MaxPartialMessageCount;
		absoluteMaxMessageSize = maxMessageSize * (allowPartialMessages ? maxPartialMessageCount : 1);
		networkMessageCharacterEncoding = owner.Options.NetworkMessageCharacterEncoding;
		Identifier = socket.HttpRequest.GetIdentifier();
		ValidatingExtension = socket.HttpRequest.GetExtension();

		_ = ReadMessagesAsync();
	}

	public void Dispose()
	{
		Dispose( disposing: true );
		GC.SuppressFinalize( this );
	}

	private void Dispose( bool disposing )
	{
		if ( disposed )
			return;

		if ( disposing )
		{
			socket.Dispose();
			disconnectSemaphore.Dispose();
			writeSemaphore.Dispose();
			clientTokenSource.Dispose();
		}

		disposed = true;
	}

	public void QueueMessage( NetworkMessage message )
	{
		ObjectDisposedException.ThrowIf( disposed, this );
		ArgumentNullException.ThrowIfNull( message );

		PendingOutgoingMessages.Enqueue( message );
		WriteThread?.Signal();

		if ( logger.IsEnabled( LoggerLevel.Debug ) )
			logger.Debug( $"Queueing {message} to send to {this}" );
	}

	public async Task DisconnectAsync()
	{
		ObjectDisposedException.ThrowIf( disposed, this );

		QueueMessage( new DisconnectMessage( ServerDisconnectReason.Forced ) );
		await DisconnectAsync( ServerDisconnectReason.Forced );
	}

	internal async Task DisconnectAsync( ServerDisconnectReason reason )
	{
		if ( DisconnectReason is not null || disconnectSemaphore.CurrentCount == 0 )
			return;

		await disconnectSemaphore.WaitAsync();

		await ProcessOutgoingMessagesAsync();
		await writeSemaphore.WaitAsync();

		clientTokenSource.Cancel();
		DisconnectReason = reason;
		await socket.CloseAsync();

		writeSemaphore.Release();
		disconnectSemaphore.Release();

		Dispose();
	}

	internal void ProcessIncomingMessages()
	{
		while ( PendingIncomingMessages.TryDequeue( out var message ) )
			OnMessageReceived?.Invoke( this, message );
	}

	internal async Task ProcessOutgoingMessagesAsync()
	{
		if ( !Connected || writeSemaphore.CurrentCount == 0 )
			return;

		await writeSemaphore.WaitAsync();

		while ( Connected && PendingOutgoingMessages.TryDequeue( out var message ) )
		{
			using var data = owner.RentArray();
			using var dataStream = new MemoryStream( data, true );

			try
			{
				NetworkMessage.WriteToStream( glue, dataStream, message, networkMessageCharacterEncoding );
			}
			catch ( Exception e )
			{
				if ( logger.IsEnabled( LoggerLevel.Error ) )
					logger.Error( $"An exception occurred during serialization of a {nameof( NetworkMessage )}", e );

				continue;
			}

			var messageSize = dataStream.Position;
			if ( messageSize > absoluteMaxMessageSize )
			{
				if ( logger.IsEnabled( LoggerLevel.Error ) )
					logger.Error( $"The message {message} ({messageSize} bytes) being sent to {this} exceeds the maximum size that can be sent ({absoluteMaxMessageSize} bytes)" );

				continue;
			}

			if ( allowPartialMessages && messageSize > maxMessageSize )
			{
				var result = await SendPartialMessagesAsync( data, messageSize );

				if ( result is null )
				{
					PendingOutgoingMessages.Clear();
					writeSemaphore.Release();

					await DisconnectAsync( ServerDisconnectReason.UnexpectedDisconnect );
					return;
				}
				else if ( !result.Value )
					continue;
			}
			else if ( !await SendMessageAsync( data.array.AsMemory( 0, (int)messageSize ) ) )
			{
				PendingOutgoingMessages.Clear();
				writeSemaphore.Release();

				await DisconnectAsync( ServerDisconnectReason.UnexpectedDisconnect );
				return;
			}

			if ( logger.IsEnabled( LoggerLevel.Debug ) )
				logger.Debug( $"Sent {message} ({messageSize} bytes) to {this}" );
		}

		writeSemaphore.Release();
	}

	private async ValueTask<bool?> SendPartialMessagesAsync( byte[] data, long messageSize )
	{
		using var partialData = owner.RentArray();

		try
		{
			foreach ( var (partialMessage, partialMessageSize) in PartialMessage.CreateFrom( glue, data, messageSize, maxMessageSize,
				networkMessageCharacterEncoding, partialData ) )
			{
				if ( !await SendMessageAsync( partialData.array.AsMemory( 0, (int)partialMessageSize ) ) )
					return null;
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

	private async ValueTask<bool> SendMessageAsync( ReadOnlyMemory<byte> data )
	{
		try
		{
			using var socketStream = socket.CreateMessageWriter( WebSocketMessageType.Binary );
			await socketStream.WriteAsync( data, clientTokenSource.Token );
			await socketStream.CloseAsync();

			return true;
		}
		catch ( WebSocketException )
		{
			return false;
		}
	}

	private async Task ReadMessagesAsync()
	{
		while ( !clientTokenSource.IsCancellationRequested )
		{
			WebSocketMessageReadStream message;
			try
			{
				message = await socket.ReadMessageAsync( clientTokenSource.Token );
			}
			catch ( OperationCanceledException ) { return; }

			if ( message is null )
			{
				await DisconnectAsync( ServerDisconnectReason.UnexpectedDisconnect );
				return;
			}

			if ( message.MessageType == WebSocketMessageType.Text )
			{
				if ( logger.IsEnabled( LoggerLevel.Warning ) )
					logger.Warning( $"Received a text message from {this} but it is not supported" );

				continue;
			}

			using var data = owner.RentArray();
			try
			{
				await message.ReadAsync( data.array, clientTokenSource.Token );
			}
			catch ( OperationCanceledException ) { return; }

			await ReceiveMessageAsync( data );
		}
	}

	private async Task ReceiveMessageAsync( byte[] data )
	{
		NetworkMessage networkMessage;
		long messageSize;
		try
		{
			networkMessage = NetworkMessage.Parse<NetworkMessage>( glue, data, networkMessageCharacterEncoding, out messageSize );
		}
		catch ( Exception e )
		{
			if ( logger.IsEnabled( LoggerLevel.Error ) )
				logger.Error( $"An exception occurred during deserialization of a {nameof( NetworkMessage )}", e );

			return;
		}

		if ( networkMessage is PartialMessage partialMessage )
		{
			await ReceivePartialMessageAsync( partialMessage );
			return;
		}

		PendingIncomingMessages.Enqueue( networkMessage );

		if ( logger.IsEnabled( LoggerLevel.Debug ) )
			logger.Debug( $"Received {networkMessage} ({messageSize} bytes) from {this}" );
	}

	private async Task ReceivePartialMessageAsync( PartialMessage partialMessage )
	{
		if ( !allowPartialMessages )
		{
			if ( logger.IsEnabled( LoggerLevel.Error ) )
				logger.Error( $"Received a partial message from {this} when they are not allowed. They will be disconnected to avoid complications" );

			await DisconnectAsync( ServerDisconnectReason.PartialMessageViolation );
			return;
		}

		if ( partialMessage.NumPieces > maxPartialMessageCount )
		{
			if ( logger.IsEnabled( LoggerLevel.Error ) )
				logger.Error( $"Received a partial message from {this} that will contain more pieces than what is allowed ({maxPartialMessageCount} allowed, {partialMessage.NumPieces} expected to received). They will be disconnected to avoid complications" );

			await DisconnectAsync( ServerDisconnectReason.PartialMessageViolation );
			return;
		}

		if ( partialReceivedCount == 0 )
			partialMessageBuffer = owner.RentArray();

		partialMessage.PartialData.CopyTo( partialMessageBuffer, maxMessageSize * partialReceivedCount );
		partialReceivedCount++;

		if ( partialReceivedCount != partialMessage.NumPieces )
			return;

		partialReceivedCount = 0;
		partialMessageBuffer.Dispose();
		await ReceiveMessageAsync( partialMessageBuffer );
	}

	public override string ToString()
	{
		return $"Client ({Identifier})";
	}
}

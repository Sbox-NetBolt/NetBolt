using NetBolt.Glue;
using NetBolt.Glue.Logging;
using NetBolt.Messaging;
using NetBolt.Messaging.Messages;
using NetBolt.Server.Extensions;
using NetBolt.Server.Util;
using NetBolt.Shared;
using NetBolt.Shared.Extensions;
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using vtortola.WebSockets;
using vtortola.WebSockets.Rfc6455;
using ILogger = NetBolt.Glue.Logging.ILogger;

namespace NetBolt.Server;

public sealed class NetBoltServer : IServerHost, IDisposable
{
	public IReadOnlySet<Client> Clients => clients;
	public EndPoint? LocalEndPoint => socketServer.LocalEndpoints.FirstOrDefault();

	[MemberNotNullWhen( true, nameof( LocalEndPoint ) )]
	public bool Active { get; private set; }

	public delegate void ClientHandler( NetBoltServer server, Client client );
	public delegate void ClientMessageHandler( NetBoltServer server, Client client, NetworkMessage message );

	public event ClientHandler? OnClientConnected;
	public event ClientHandler? OnClientDisconnected;
	public event ClientMessageHandler? OnClientMessageReceived;

	public StringCache StringCache => Glue.StringCache;
	public ExtensionContainer<IExtension> Extensions { get; }

	internal INetBoltGlue Glue { get; }
	internal NetBoltServerOptions Options { get; }

	internal CancellationTokenSource ServerTokenSource { get; } = new();

	private bool disposed;

	private readonly ILogger logger;
	private readonly WebSocketListenerOptions socketOptions;
	private readonly WebSocketListener socketServer;
	private readonly Thread clientAcceptThread;
	private readonly int maxClients;
	private readonly int maxClientsPerWriteThread;
	private readonly int messageBufferSize;
	private readonly bool stringCacheEnabled;
	private readonly ArrayPool<byte> sendReceiveDataPool;
	private readonly ConcurrentBag<WriteThread> writeThreads = [];

	private readonly ConcurrentQueue<Client> clientsWaitingToJoin = new();
	private readonly ConcurrentQueue<Client> clientsWaitingToLeave = new();
	private readonly ConcurrentHashSet<Client> clients = [];

	private readonly object startLock = new();
	private readonly SemaphoreSlim stopSemaphore = new( 1 );

	public NetBoltServer() : this( NetBoltServerOptions.Default )
	{
	}

	public NetBoltServer( NetBoltServerOptions options )
	{
		ArgumentNullException.ThrowIfNull( options );

		options.Validate();
		Options = new NetBoltServerOptions( options );

		if ( options.Glue is not null )
			Glue = options.Glue;
		else
			Glue = new DefaultGlue( options.Logger, this, options.StringCacheEnabled );

		if ( options.StringCacheEnabled )
		{
			foreach ( var assembly in AppDomain.CurrentDomain.GetAssemblies() )
			{
				foreach ( var type in assembly.DefinedTypes )
				{
					if ( !type.IsAssignableTo( typeof( NetworkMessage ) ) || type.IsAbstract )
						continue;

					Glue.StringCache.Add( type );
				}
			}
		}

		logger = options.Logger;
		Extensions = options.Extensions;
		foreach ( var extension in Extensions.OfType<NetBoltServerExtension>() )
		{
			extension.Server = this;
			extension.Logger = options.Logger;
		}

		socketOptions = new WebSocketListenerOptions
		{
			HttpAuthenticationHandler = NegotiateSocketAsync,
			PingMode = PingMode.LatencyControl,
			PingTimeout = options.PingTimeout,
			SendBufferSize = options.MaxMessageSize,
			BufferManager = BufferManager.CreateBufferManager(
				(options.MaxMessageSize + 1024) * options.MaxClients,
				options.MaxMessageSize + 1024 ),
			Logger = options.Logger is vtortola.WebSockets.ILogger webSocketLogger
				? webSocketLogger
				: vtortola.WebSockets.NullLogger.Instance
		};
		socketOptions.Standards.RegisterRfc6455();
		socketServer = new WebSocketListener( options.IPEndPoint, socketOptions );

		clientAcceptThread = new Thread( ListenForClientsThreadLoopAsync )
		{
			Name = "NetBolt Client listener"
		};

		maxClients = options.MaxClients;
		maxClientsPerWriteThread = options.MaxClientsPerWriteThread;
		messageBufferSize = options.MaxMessageSize * (options.AllowPartialMessages ? Options.MaxPartialMessageCount : 1);
		stringCacheEnabled = options.StringCacheEnabled;
		sendReceiveDataPool = ArrayPool<byte>.Create( messageBufferSize, options.MaxClients * 2 * (Options.AllowPartialMessages ? 2 : 1) );
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
			ServerTokenSource.Dispose();
			socketServer.Dispose();
			stopSemaphore.Dispose();
		}

		Glue.StringCache.OnChanged -= OnStringCacheChanged;
		disposed = true;
	}

	public void Start()
	{
		ObjectDisposedException.ThrowIf( disposed, this );

		if ( Active || socketServer.IsStarted )
			throw new InvalidOperationException( "The server is currently running" );

		if ( !Monitor.TryEnter( startLock ) )
			return;

		if ( logger.IsEnabled( LoggerLevel.Debug ) )
			logger.Debug( "Starting server..." );

		Client.OnMessageReceived += InternalOnClientMessageReceived;
		_ = socketServer.StartAsync();
		clientAcceptThread.Start();
		Active = true;
		Glue.StringCache.OnChanged += OnStringCacheChanged;

		if ( logger.IsEnabled( LoggerLevel.Debug ) )
			logger.Debug( $"Server started on {LocalEndPoint}" );

		foreach ( var extension in Extensions )
			extension.Start();

		Monitor.Exit( startLock );
	}

	public async Task StopAsync()
	{
		ObjectDisposedException.ThrowIf( disposed, this );

		if ( !Active || !socketServer.IsStarted )
			throw new InvalidOperationException( "The server is not currently running" );

		if ( stopSemaphore.CurrentCount == 0 )
			return;

		await stopSemaphore.WaitAsync();

		if ( logger.IsEnabled( LoggerLevel.Debug ) )
			logger.Debug( "Stopping server..." );

		foreach ( var extension in Extensions )
			extension.Stop();

		Client.OnMessageReceived -= InternalOnClientMessageReceived;
		Active = false;
		ServerTokenSource.Cancel();

		clientAcceptThread.Join();
		clientsWaitingToJoin.Clear();

		foreach ( var client in clients )
			client.QueueMessage( new DisconnectMessage( ServerDisconnectReason.Shutdown ) );

		foreach ( var writeThread in writeThreads )
			writeThread.Join();

		var disconnectTasks = new List<Task>( clients.Count );
		foreach ( var client in clients )
		{
			disconnectTasks.Add( client.DisconnectAsync( ServerDisconnectReason.Shutdown ) );
			InternalOnClientDisconnected( client );
		}
		await Task.WhenAll( disconnectTasks );

		disconnectTasks.Clear();
		clients.Clear();

		foreach ( var writeThread in writeThreads )
			writeThread.Dispose();

		writeThreads.Clear();

		stopSemaphore.Release();

		Dispose();

		if ( logger.IsEnabled( LoggerLevel.Debug ) )
			logger.Debug( "Server stopped" );
	}

	public void ProcessAllEvents()
	{
		ObjectDisposedException.ThrowIf( disposed, this );

		ProcessIncomingClients();

		foreach ( var client in clients )
			ProcessClientEvents( client );

		foreach ( var extension in Extensions )
			ProcessExtensionEvents( extension );

		ProcessOutgoingClients();
	}

	public void ProcessIncomingClients()
	{
		ObjectDisposedException.ThrowIf( disposed, this );

		while ( clientsWaitingToJoin.TryDequeue( out var client ) )
		{
			clients.Add( client );

			InternalOnClientConnected( client );
		}
	}

	public void ProcessOutgoingClients()
	{
		ObjectDisposedException.ThrowIf( disposed, this );

		while ( clientsWaitingToLeave.TryDequeue( out var client ) )
		{
			clients.Remove( client );

			client.DisconnectReason ??= ServerDisconnectReason.UnexpectedDisconnect;
			InternalOnClientDisconnected( client );
		}
	}

	public void ProcessClientEvents( Client client )
	{
		ObjectDisposedException.ThrowIf( disposed, this );

		ArgumentNullException.ThrowIfNull( client );

		client.ProcessIncomingMessages();
		if ( client.Connected && client.DisconnectReason is null )
			return;

		clientsWaitingToLeave.Enqueue( client );
	}

	public void ProcessExtensionEvents( IExtension extension )
	{
		try
		{
			extension.ProcessEvents();
		}
		catch ( Exception e )
		{
			if ( logger.IsEnabled( LoggerLevel.Error ) )
				logger.Error( $"An exception occurred during invocation of {nameof( NetBoltServerExtension.ProcessEvents )} on the {extension.Name} extension", e );
		}
	}

	public Client? GetClientByIdentifier( in ClientIdentifier identifier )
	{
		ObjectDisposedException.ThrowIf( disposed, this );

		var foundClient = default( Client );

		foreach ( var client in clients )
		{
			if ( client.Identifier != identifier )
				continue;

			foundClient = client;
			break;
		}

		return foundClient;
	}

	internal TemporaryArrayAccess<byte> RentArray()
	{
		return new( sendReceiveDataPool.Rent( messageBufferSize ), ReturnArray );
	}

	private void ReturnArray( TemporaryArrayAccess<byte> array )
	{
		sendReceiveDataPool.Return( array );
	}

	private void InternalOnClientConnected( Client client )
	{
		if ( writeThreads.Count * maxClientsPerWriteThread < clients.Count )
		{
			var writeThread = new WriteThread( this );
			writeThread.AddClient( client );
			writeThreads.Add( writeThread );
		}
		else
		{
			foreach ( var writeThread in writeThreads )
			{
				if ( writeThread.ClientCount >= maxClientsPerWriteThread )
					continue;

				writeThread.AddClient( client );
				break;
			}
		}

		if ( stringCacheEnabled )
			client.QueueMessage( new StringCacheUpdateMessage( Glue, StringCache.Entries ) );

		if ( logger.IsEnabled( LoggerLevel.Information ) )
			logger.Information( $"{client} has joined" );

		foreach ( var extension in Extensions.OfType<NetBoltServerExtension>() )
		{
			try
			{
				extension.OnClientConnected( client );
			}
			catch ( Exception e )
			{
				if ( logger.IsEnabled( LoggerLevel.Error ) )
					logger.Error( $"An exception occurred during invocation of {nameof( NetBoltServerExtension.OnClientConnected )} on the {extension.Name} extension", e );
			}
		}

		OnClientConnected?.Invoke( this, client );
	}

	private void InternalOnClientDisconnected( Client client )
	{
		foreach ( var writeThread in writeThreads )
		{
			if ( !writeThread.HasClient( client ) )
				continue;

			writeThread.RemoveClient( client );
			break;
		}

		if ( logger.IsEnabled( LoggerLevel.Information ) )
			logger.Information( $"{client} has left for reason: {client.DisconnectReason}" );

		foreach ( var extension in Extensions.OfType<NetBoltServerExtension>() )
		{
			try
			{
				extension.OnClientDisconnected( client );
			}
			catch (Exception e )
			{
				if ( logger.IsEnabled( LoggerLevel.Error ) )
					logger.Error( $"An exception occurred during invocation of {nameof( NetBoltServerExtension.OnClientDisconnected )} on the {extension.Name} extension", e );
			}
		}

		OnClientDisconnected?.Invoke( this, client );
	}

	private void InternalOnClientMessageReceived( Client client, NetworkMessage message )
	{
		switch ( message )
		{
			case DisconnectMessage:
				client.QueueMessage( message );
				_ = client.DisconnectAsync( ServerDisconnectReason.Requested );
				return;
		}

		foreach ( var extension in Extensions.OfType<NetBoltServerExtension>() )
		{
			try
			{
				if ( extension.OnClientMessageReceived( client, message ) )
					return;
			}
			catch ( Exception e )
			{
				if ( logger.IsEnabled( LoggerLevel.Error ) )
					logger.Error( $"An exception occurred during invocation of {nameof( NetBoltServerExtension.OnClientMessageReceived )} on the {extension.Name} extension", e );
			}
		}

		OnClientMessageReceived?.Invoke( this, client, message );
	}

	private void OnStringCacheChanged( StringCache cache )
	{
		var updateMessage = new StringCacheUpdateMessage( Glue, cache.Entries );
		foreach ( var client in clients )
			client.QueueMessage( updateMessage );
	}

	private async void ListenForClientsThreadLoopAsync()
	{
		while ( !ServerTokenSource.IsCancellationRequested )
		{
			try
			{
				var clientSocket = await socketServer.AcceptWebSocketAsync( CancellationToken.None );
				if ( clientSocket is null )
					continue;

				clientsWaitingToJoin.Enqueue( new Client( this, clientSocket ) );
			}
			catch ( InvalidOperationException e )
			{
				if ( logger.IsEnabled( LoggerLevel.Debug ) )
					logger.Debug( "An exception occurred during the handshake of a web socket connection", e );
			}
			catch ( IOException e )
			{
				if ( logger.IsEnabled( LoggerLevel.Debug ) )
					logger.Debug( "An exception occurred during the handshake of a web socket connection", e );
			}
			catch ( WebSocketException e )
			{
				if ( logger.IsEnabled( LoggerLevel.Error ) )
					logger.Error( "An exception occurred during the handshake of a web socket connection", e );
			}
		}
	}

	private async Task<bool> NegotiateSocketAsync( WebSocketHttpRequest request, WebSocketHttpResponse response )
	{
		if ( clients.Count >= maxClients )
		{
			if ( logger.IsEnabled( LoggerLevel.Warning ) )
				logger.Warning( $"Refusing connection from {request.RemoteEndPoint} due to the server being full" );

			response.Status = HttpStatusCode.InsufficientStorage;
			return false;
		}

		if ( !request.Headers.Contains( "User-Agent" ) )
		{
			if ( logger.IsEnabled( LoggerLevel.Warning ) )
				logger.Warning( $"Refusing connection from {request.RemoteEndPoint} due to not receiving a User-Agent header" );

			response.Status = HttpStatusCode.BadRequest;
			return false;
		}

		foreach ( var extension in Extensions.OfType<NetBoltServerExtension>() )
		{
			try
			{
				if ( !await extension.OnNegotiateSocketAsync( request, response ) )
				{
					logger.Warning( $"Refusing connection from {request.RemoteEndPoint} due to being refused by {extension}" );
					return false;
				}
			}
			catch ( Exception e )
			{
				if ( logger.IsEnabled( LoggerLevel.Error ) )
					logger.Error( $"An exception occurred during invocation of the {nameof( NetBoltServerExtension.OnNegotiateSocketAsync )} event of the {extension.Name} extension", e );
			}
		}

		if ( !request.HasIdentifier() )
			throw new InvalidOperationException( "No identifier was stored for the socket. Are you missing an extension to find this for you?" );

		return true;
	}

	void IServerHost.SendMessageTo( IClient client, NetworkMessage message )
	{
		((Client)client).QueueMessage( message );
	}
}

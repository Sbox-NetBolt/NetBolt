using NetBolt.Glue.Logging;
using NetBolt.Messaging;
using NetBolt.Messaging.Messages;
using NetBolt.Server.Extensions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using vtortola.WebSockets;

namespace NetBolt.Server;

public sealed class SboxExtension : NetBoltServerExtension
{
	private const string SboxUserAgent = "facepunch-sbox";
	private const string SteamIdHeader = "steamid";
	private const string SboxAuthTokenHeader = "token";

	public override string Name => "S&box";

	private ConcurrentDictionary<Client, DateTime> LastTokenChecks { get; } = new();

	private readonly TimeSpan tokenCheckInterval;
	private readonly TimeSpan tokenTimeout;

	public SboxExtension() : this( SboxExtensionOptions.Default )
	{
	}

	public SboxExtension( SboxExtensionOptions options )
	{
		options.Validate();

		tokenCheckInterval = options.TokenCheckInterval;
		tokenTimeout = options.TokenTimeout;
	}

	public override void OnClientConnected( Client client )
	{
		if ( !ReferenceEquals( this, client.ValidatingExtension ) )
			return;

		LastTokenChecks.TryAdd( client, DateTime.UtcNow );
		_ = UpdateTokenLoopAsync( client );
	}

	public override void OnClientDisconnected( Client client )
	{
		if ( !ReferenceEquals( this, client.ValidatingExtension ) )
			return;

		LastTokenChecks.TryRemove( client, out _ );
	}

	public override bool OnClientMessageReceived( Client client, NetworkMessage message )
	{
		if ( message is not SboxTokenRequestMessage tokenRequestMessage )
			return false;

		_ = ValidateSboxTokenAsync( client, tokenRequestMessage );
		return true;
	}

	public override async ValueTask<bool> OnNegotiateSocketAsync( WebSocketHttpRequest request, WebSocketHttpResponse response )
	{
		if ( request.Headers["User-Agent"] != SboxUserAgent )
			return true;

		if ( !request.Headers.Contains( SteamIdHeader ) || !request.Headers.Contains( SboxAuthTokenHeader ) )
		{
			if ( Logger.IsEnabled( LoggerLevel.Warning ) )
				Logger.Warning( $"Refusing connection from {request.RemoteEndPoint} due to not being given required S&box identification" );

			response.Status = HttpStatusCode.Unauthorized;
			return false;
		}

		if ( !long.TryParse( request.Headers[SteamIdHeader], out var steamId ) )
		{
			if ( Logger.IsEnabled( LoggerLevel.Warning ) )
				Logger.Warning( $"Refusing connection from {request.RemoteEndPoint} due to receiving an invalid SteamID64" );

			response.Status = HttpStatusCode.BadRequest;
			return false;
		}

		if ( !await ValidateSboxTokenAsync( steamId, request.Headers[SboxAuthTokenHeader] ) )
		{
			if ( Logger.IsEnabled( LoggerLevel.Warning ) )
				Logger.Warning( $"Refusing connection from {request.RemoteEndPoint} due to being unable to validate the provided S&box token" );

			response.Status = HttpStatusCode.Unauthorized;
			return false;
		}

		var identifier = new ClientIdentifier( Platform.Steam, steamId );
		foreach ( var client in Server.Clients )
		{
			if ( client.Identifier != identifier )
				continue;

			if ( Logger.IsEnabled( LoggerLevel.Warning ) )
				Logger.Warning( $"Refusing connection from {request.RemoteEndPoint} due to another client with the same identifier ({identifier}) being in the server" );

			response.Status = HttpStatusCode.Unauthorized;
			return false;
		}
	
		request.StoreIdentifier( identifier, this );
		return true;
	}

	private async Task UpdateTokenLoopAsync( Client client )
	{
		while ( client.Connected )
		{
			await Task.Delay( tokenCheckInterval );
			if ( !client.Connected )
				return;

			client.QueueMessage( new SboxTokenRequestMessage() );

			var lastCheck = LastTokenChecks[client];
			while ( client.Connected && LastTokenChecks[client] == lastCheck )
			{
				await Task.Delay( 1 );
				if ( DateTime.UtcNow - lastCheck < tokenTimeout )
					continue;

				await client.DisconnectAsync();
				return;
			}
		}
	}

	private async Task ValidateSboxTokenAsync( Client client, SboxTokenRequestMessage tokenRequestMessage )
	{
		if ( tokenRequestMessage.Token is null )
		{
			if ( Logger.IsEnabled( LoggerLevel.Warning ) )
				Logger.Warning( $"Refused connection from {client} due to receiving no S&box token to validate" );

			await client.DisconnectAsync( ServerDisconnectReason.InvalidToken );
			return;
		}

		if ( !await ValidateSboxTokenAsync( client.Identifier.Identifier, tokenRequestMessage.Token ) )
		{
			if ( Logger.IsEnabled( LoggerLevel.Warning ) )
				Logger.Warning( $"Refused connection from {client} due to being unable to validate the provided S&box token" );

			await client.DisconnectAsync( ServerDisconnectReason.InvalidToken );
			return;
		}

		LastTokenChecks[client] = DateTime.UtcNow;
	}

	private static async ValueTask<bool> ValidateSboxTokenAsync( long steamId, string token )
	{
		var client = new HttpClient();
		var data = new Dictionary<string, object>
		{
			{ SteamIdHeader, steamId },
			{ SboxAuthTokenHeader, token }
		};
		var content = new StringContent( JsonSerializer.Serialize( data ), Encoding.UTF8, "application/json" );

		var result = await client.PostAsync( "https://services.facepunch.com/sbox/auth/token", content );
		if ( result.StatusCode != HttpStatusCode.OK )
			return false;

		var response = await result.Content.ReadFromJsonAsync<SboxAuthTokenResponse>();
		if ( !response.IsComplete || response.Status != "ok" )
			return false;

		return response.SteamId == steamId;
	}
}

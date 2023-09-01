using System;
using System.Collections.Generic;
using vtortola.WebSockets;

namespace NetBolt.Server.Extensions;

public static class WebSocketHttpRequestExtensions
{
	private const string NetBoltHttpIdentifier = "netbolt_identifier";
	private const string NetBoltHttpExtension = "netbolt_extension";

	public static void StoreIdentifier( this WebSocketHttpRequest request, in ClientIdentifier clientIdentifier, NetBoltServerExtension extension )
	{
		if ( HasIdentifier( request ) )
			throw new InvalidOperationException( "An identifier has already been stored for this request" );

		request.Items.Add( NetBoltHttpIdentifier, clientIdentifier );
		request.Items.Add( NetBoltHttpExtension, extension );
	}

	public static ClientIdentifier GetIdentifier( this WebSocketHttpRequest request )
	{
		if ( !request.Items.TryGetValue( NetBoltHttpIdentifier, out var identifier ) )
			throw new KeyNotFoundException( "No identifier was stored for this request" );

		if ( identifier is not ClientIdentifier clientIdentifier )
			throw new InvalidCastException( $"An invalid value ({identifier}) was stored in the identifier slot" );

		return clientIdentifier;
	}

	public static NetBoltServerExtension GetExtension( this WebSocketHttpRequest request )
	{
		if ( !request.Items.TryGetValue( NetBoltHttpExtension, out var extension ) )
			throw new KeyNotFoundException( "No extension was stored for this request" );

		if ( extension is not NetBoltServerExtension foundExtension )
			throw new InvalidCastException( $"An invalid value ({extension}) was stored in the identifier slot" );

		return foundExtension;
	}

	public static bool HasIdentifier( this WebSocketHttpRequest request )
	{
		return request.Items.ContainsKey( NetBoltHttpIdentifier ) && request.Items[NetBoltHttpIdentifier] is ClientIdentifier;
	}
}

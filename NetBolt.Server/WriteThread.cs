using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NetBolt.Server;

internal sealed class WriteThread : IDisposable
{
	internal int ClientCount => clients.Count + clientsToAdd.Count - clientsToRemove.Count;

	private bool disposed;

	private readonly NetBoltServer owner;
	private readonly Thread workerThread;
	private readonly List<Task> writeTasks;

	private readonly Queue<Client> clientsToAdd = new();
	private readonly Queue<Client> clientsToRemove = new();
	private readonly HashSet<Client> clients = new();

	private readonly object queueLock = new();
	private readonly AutoResetEvent writeEvent = new( false );

	internal WriteThread( NetBoltServer owner )
	{
		this.owner = owner;

		writeTasks = new List<Task>( owner.Options.MaxClientsPerWriteThread );

		workerThread = new Thread( ThreadLoopAsync )
		{
			Name = "NetBolt write thread"
		};
		workerThread.Start();
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
			writeEvent.Dispose();

		disposed = true;
	}

	internal void AddClient( Client client )
	{
		if ( disposed )
			throw new ObjectDisposedException( nameof( WriteThread ) );

		if ( HasClient( client ) )
			throw new ArgumentException( $"{client} is already in this {nameof( WriteThread )}", nameof( client ) );

		if ( clients.Contains( client ) )
		{
			RewriteQueue( clientsToRemove, client );
			writeEvent.Set();
			return;
		}

		lock ( queueLock )
			clientsToAdd.Enqueue( client );

		writeEvent.Set();
	}

	internal void RemoveClient( Client client )
	{
		if ( disposed )
			throw new ObjectDisposedException( nameof( WriteThread ) );

		if ( !HasClient( client ) )
			throw new ArgumentException( $"{client} is not contained in this {nameof( WriteThread )}", nameof( client ) );

		if ( !clients.Contains( client ) )
		{
			RewriteQueue( clientsToAdd, client );
			writeEvent.Set();
			return;
		}

		lock ( queueLock )
			clientsToRemove.Enqueue( client );

		writeEvent.Set();
	}

	internal bool HasClient( Client client )
	{
		if ( disposed )
			throw new ObjectDisposedException( nameof( WriteThread ) );

		return (clients.Contains( client ) || clientsToAdd.Contains( client )) && !clientsToRemove.Contains( client );
	}

	internal void Signal()
	{
		if ( disposed )
			throw new ObjectDisposedException( nameof( WriteThread ) );

		writeEvent.Set();
	}

	internal void Join()
	{
		if ( disposed )
			throw new ObjectDisposedException( nameof( WriteThread ) );

		workerThread.Join();
	}

	private async void ThreadLoopAsync()
	{
		while ( !owner.ServerTokenSource.IsCancellationRequested )
		{
			if ( !writeEvent.WaitOne( 1 ) )
				continue;

			lock ( queueLock )
			{
				while ( clientsToRemove.TryDequeue( out var client ) )
					clients.Remove( client );

				while ( clientsToAdd.TryDequeue( out var client ) )
				{
					clients.Add( client );
					client.WriteThread = this;
				}
			}

			await WriteMessagesAsync();
		}

		await WriteMessagesAsync();
	}

	private async Task WriteMessagesAsync()
	{
		foreach ( var client in clients )
		{
			if ( clientsToRemove.Contains( client ) )
				continue;

			writeTasks.Add( client.ProcessOutgoingMessagesAsync() );
		}

		await Task.WhenAll( writeTasks );
		writeTasks.Clear();
	}

	private void RewriteQueue( Queue<Client> queue, Client toIgnore )
	{
		var contents = new Client[queue.Count];
		lock ( queueLock )
		{
			for ( var i = queue.Count - 1; i >= 0; i-- )
				contents[i] = queue.Dequeue();

			for ( var i = 0; i < contents.Length; i++ )
			{
				if ( contents[i] == toIgnore )
					continue;

				queue.Enqueue( contents[i] );
			}
		}
	}
}

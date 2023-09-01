using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace NetBolt.Server.Util;

internal sealed class ConcurrentHashSet<T> : ISet<T>, IReadOnlySet<T> where T : notnull
{
	public int Count => inner.Count;
	public bool IsReadOnly => false;

	private readonly ConcurrentDictionary<T, byte> inner;

	public ConcurrentHashSet()
	{
		inner = new ConcurrentDictionary<T, byte>();
	}

	public ConcurrentHashSet( IEqualityComparer<T> comparer )
	{
		inner = new ConcurrentDictionary<T, byte>( comparer );
	}

	public bool Add( T item ) => inner.TryAdd( item, 0 );
	public bool Remove( T item ) => inner.TryRemove( item, out _ );
	public bool Contains( T item ) => inner.ContainsKey( item );
	public void Clear() => inner.Clear();
	public IEnumerator<T> GetEnumerator() => inner.Keys.GetEnumerator();

	void ICollection<T>.Add( T item ) => Add( item );
	void ICollection<T>.CopyTo( T[] array, int arrayIndex ) => inner.Keys.CopyTo( array, arrayIndex );
	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

	void ISet<T>.ExceptWith( IEnumerable<T> other ) => throw new NotSupportedException();
	void ISet<T>.IntersectWith( IEnumerable<T> other ) => throw new NotSupportedException();
	bool ISet<T>.IsProperSubsetOf( IEnumerable<T> other ) => throw new NotSupportedException();
	bool ISet<T>.IsProperSupersetOf( IEnumerable<T> other ) => throw new NotSupportedException();
	bool ISet<T>.IsSubsetOf( IEnumerable<T> other ) => throw new NotSupportedException();
	bool ISet<T>.IsSupersetOf( IEnumerable<T> other ) => throw new NotSupportedException();
	bool ISet<T>.Overlaps( IEnumerable<T> other ) => throw new NotSupportedException();
	bool ISet<T>.SetEquals( IEnumerable<T> other ) => throw new NotSupportedException();
	void ISet<T>.SymmetricExceptWith( IEnumerable<T> other ) => throw new NotSupportedException();
	void ISet<T>.UnionWith( IEnumerable<T> other ) => throw new NotSupportedException();

	bool IReadOnlySet<T>.IsProperSubsetOf( IEnumerable<T> other ) => throw new NotSupportedException();
	bool IReadOnlySet<T>.IsProperSupersetOf( IEnumerable<T> other ) => throw new NotSupportedException();
	bool IReadOnlySet<T>.IsSubsetOf( IEnumerable<T> other ) => throw new NotSupportedException();
	bool IReadOnlySet<T>.IsSupersetOf( IEnumerable<T> other ) => throw new NotSupportedException();
	bool IReadOnlySet<T>.Overlaps( IEnumerable<T> other ) => throw new NotSupportedException();
	bool IReadOnlySet<T>.SetEquals( IEnumerable<T> other ) => throw new NotSupportedException();
}

using System;

namespace NetBolt.Server.Util
{
	internal readonly struct TemporaryArrayAccess<T> : IDisposable
	{
		internal readonly T[] array;
		private readonly Action<TemporaryArrayAccess<T>> disposeCb;

		internal TemporaryArrayAccess( T[] array, Action<TemporaryArrayAccess<T>> disposeCb )
		{
			this.array = array;
			this.disposeCb = disposeCb;
		}

		public void Dispose()
		{
			disposeCb( this );
		}

		public static implicit operator T[]( in TemporaryArrayAccess<T> tempArray ) => tempArray.array;
	}
}

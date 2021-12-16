﻿namespace Ecng.Net
{
	using System;
	using System.Net.Http;

	using Ecng.Common;
	using Ecng.Collections;

	public class InMemoryRestApiClientCache : IRestApiClientCache
	{
		private readonly SynchronizedDictionary<(HttpMethod, string), (object value, DateTime till)> _cache = new();
		private readonly TimeSpan _timeout;

		public InMemoryRestApiClientCache(TimeSpan timeout)
		{
			if (timeout <= TimeSpan.Zero)
				throw new ArgumentOutOfRangeException(nameof(timeout));

			_timeout = timeout;
		}

		private (HttpMethod, string) ToKey(HttpMethod method, Uri uri)
			=> (method.CheckOnNull(nameof(method)), uri.CheckOnNull(nameof(uri)).To<string>().ToLowerInvariant());

		private bool IsSupported(HttpMethod method) => method == HttpMethod.Get;

		void IRestApiClientCache.Set<T>(HttpMethod method, Uri uri, T value)
		{
			if (value is null || !IsSupported(method))
				return;

			_cache[ToKey(method, uri)] = new(value, DateTime.UtcNow + _timeout);
		}

		bool IRestApiClientCache.TryGet<T>(HttpMethod method, Uri uri, out T value)
		{
			value = default;

			var key = ToKey(method, uri);

			if (!IsSupported(method) || !_cache.TryGetValue(key, out var tuple))
				return false;

			if (tuple.till < DateTime.UtcNow)
			{
				_cache.Remove(key);
				return false;
			}

			value = (T)tuple.value;
			return true;
		}

		void IRestApiClientCache.Clear() => _cache.Clear();
		bool IRestApiClientCache.Remove(HttpMethod method, Uri uri) => _cache.Remove(ToKey(method, uri));
	}
}
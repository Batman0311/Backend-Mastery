using System.Collections.Concurrent;
using ResolverFactory.Services;

namespace ResolverFactory.Resolvers;

public sealed class ConcurrentFactoryResolver
{
	// ConcurrentDictionary prevents structural races on registration and lookup.
	private readonly ConcurrentDictionary<string, Func<IService>> _factories = new();
	// Cache is thread-safe, but GetOrAdd can still invoke the factory multiple times.
	private readonly ConcurrentDictionary<string, IService> _cache = new();

	public void Register(string key, Func<IService> factory)
	{
		// Thread-safe registration for the factory delegate.
		_factories[key] = factory;
	}

	public IService Resolve(string key)
	{
		// Issue: valueFactory may run more than once under contention.
		return _cache.GetOrAdd(key, _ => _factories[key]());
	}
}

using System.Collections.Concurrent;
using ResolverFactory.Services;

namespace ResolverFactory.Resolvers;

public sealed class LazyFactoryResolver
{
	// Factories are still stored in a thread-safe dictionary.
	private readonly ConcurrentDictionary<string, Func<IService>> _factories = new();
	// Cache stores Lazy so only one initialization wins.
	private readonly ConcurrentDictionary<string, Lazy<IService>> _cache = new();

	public void Register(string key, Func<IService> factory)
	{
		// Registration is thread-safe and can be called at startup.
		_factories[key] = factory;
	}

	public IService Resolve(string key)
	{
		// Fix: Lazy ensures the factory executes once even if multiple threads race.
		var lazy = _cache.GetOrAdd(
			key,
			_ => new Lazy<IService>(
				() => _factories[key](),
				LazyThreadSafetyMode.ExecutionAndPublication));

		// Accessing Value triggers creation exactly once and then caches the instance.
		return lazy.Value;
	}
}

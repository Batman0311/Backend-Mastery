using ResolverFactory.Services;

namespace ResolverFactory.Resolvers;

public sealed class NaiveFactoryResolver
{
	// Not thread-safe: Dictionary can be mutated and read concurrently.
	private readonly Dictionary<string, Func<IService>> _factories = new();
	// Not thread-safe: race between TryGetValue and write causes double creation.
	private readonly Dictionary<string, IService> _cache = new();

	public void Register(string key, Func<IService> factory)
	{
		// Overwrites are allowed; no validation to keep the demo minimal.
		_factories[key] = factory;
	}

	public IService Resolve(string key)
	{
		// Race: two threads can both miss the cache and create duplicates.
		if (_cache.TryGetValue(key, out var cached))
		{
			return cached;
		}

		// The timing window is between the cache miss and cache write.
		var created = _factories[key]();
		_cache[key] = created;
		return created;
	}
}

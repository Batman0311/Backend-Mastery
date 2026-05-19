using ResolverFactory.Services;

namespace ResolverFactory.Resolvers;

public sealed class IfElseFactory
{
	public IService Create(string key)
	{
		// Simple branching logic: useful for very small sets of options.
		if (string.Equals(key, "pricing", StringComparison.OrdinalIgnoreCase))
		{
			return ExpensiveService.Create("Pricing");
		}

		// Additional branch for another known key.
		if (string.Equals(key, "shipping", StringComparison.OrdinalIgnoreCase))
		{
			return ExpensiveService.Create("Shipping");
		}

		// If/else chains scale poorly as keys grow; a resolver registry is preferred.
		throw new ArgumentException($"Unknown service key '{key}'.", nameof(key));
	}
}

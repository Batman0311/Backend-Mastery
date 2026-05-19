using System.Collections.Concurrent;
using ResolverFactory.Resolvers;
using ResolverFactory.Services;

namespace ResolverFactory.Demos;

public static class ResolverDemo
{
	// Production scenario: choosing handlers based on a routing key.
	public static void RunIfElseFactory()
	{
		// Reset the static counter so each demo starts from a clean baseline.
		ExpensiveService.ResetCounter();
		// Simple factory with explicit if/else selection logic.
		var factory = new IfElseFactory();

		// These calls are intentionally sequential to focus on selection logic, not concurrency.
		var pricing = factory.Create("pricing");
		var shipping = factory.Create("shipping");

		// Output shows which concrete service was selected by the key.
		Console.WriteLine("Mode: ifelse");
		Console.WriteLine($"Pricing: {pricing.Name} #{pricing.InstanceId}");
		Console.WriteLine($"Shipping: {shipping.Name} #{shipping.InstanceId}");
	}

	// Production scenario: resolving pricing calculators during burst traffic.
	public static void RunNaiveRace()
	{
		// Reset counter so we can measure how many instances were constructed.
		ExpensiveService.ResetCounter();
		// Naive resolver uses Dictionary without synchronization.
		var resolver = new NaiveFactoryResolver();
		// Register a factory for the key that will be resolved concurrently.
		resolver.Register("pricing", () => ExpensiveService.Create("Pricing"));

		// Bag collects results across threads without locking.
		var instances = new ConcurrentBag<IService>();

		// Timing window: read-check-create-write is not synchronized.
		Parallel.For(0, 50, _ =>
		{
			// Multiple threads can observe a missing cache entry and create duplicates.
			instances.Add(resolver.Resolve("pricing"));
		});

		// Summary highlights double-initialization and distinct instance count.
		PrintSummary("naive", instances);
	}

	// Production scenario: compiling templates or rules on demand.
	public static void RunGetOrAddBug()
	{
		// Reset counter so we can count factory invocations accurately.
		ExpensiveService.ResetCounter();
		// ConcurrentDictionary prevents structural races but not duplicate factory execution.
		var resolver = new ConcurrentFactoryResolver();
		// Factory has side effects (expensive creation) so duplicate runs are visible.
		resolver.Register("pricing", () => ExpensiveService.Create("Pricing"));

		var instances = new ConcurrentBag<IService>();

		// Timing window: valueFactory can run multiple times under contention.
		Parallel.For(0, 50, _ =>
		{
			// Only one value is stored, but multiple creations can still occur.
			instances.Add(resolver.Resolve("pricing"));
		});

		// Summary shows that CreatedCount can exceed distinct resolved instances.
		PrintSummary("getoradd-bug", instances);
	}

	// Production scenario: singleton service creation under load.
	public static void RunLazyFixed()
	{
		// Reset counter so we can verify the fix creates only one instance.
		ExpensiveService.ResetCounter();
		// Lazy resolver wraps the factory to guarantee a single execution.
		var resolver = new LazyFactoryResolver();
		// Register a factory for the shared key.
		resolver.Register("pricing", () => ExpensiveService.Create("Pricing"));

		var instances = new ConcurrentBag<IService>();

		Parallel.For(0, 50, _ =>
		{
			// Lazy.Value ensures only one thread performs initialization.
			instances.Add(resolver.Resolve("pricing"));
		});

		// Summary confirms single creation and single distinct instance.
		PrintSummary("lazy-fixed", instances);
	}

	private static void PrintSummary(string mode, ConcurrentBag<IService> instances)
	{
		// Distinct instance IDs show how many unique objects were returned.
		var distinct = instances.Select(service => service.InstanceId).Distinct().Count();
		Console.WriteLine($"Mode: {mode}");
		// CreatedCount reveals how many times the factory was executed.
		Console.WriteLine($"Created instances: {ExpensiveService.CreatedCount}");
		// Distinct resolved shows how many unique services callers actually saw.
		Console.WriteLine($"Distinct resolved: {distinct}");
	}
}

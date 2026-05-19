namespace ResolverFactory.Services;

public sealed class ExpensiveService : IService
{
	// Shared counter lets the demos observe how many creations happened.
	private static int _created;

	// Read the counter with a volatile read to reflect latest value across threads.
	public static int CreatedCount => Volatile.Read(ref _created);

	// Name identifies the logical service returned by the factory.
	public string Name { get; }
	// InstanceId distinguishes each constructed object.
	public int InstanceId { get; }

	// Private constructor enforces creation via the factory method.
	private ExpensiveService(string name, int instanceId)
	{
		Name = name;
		InstanceId = instanceId;
	}

	public static ExpensiveService Create(string name)
	{
		// Simulate expensive construction and widen the contention window.
		Thread.Sleep(25);
		// Increment is atomic so the counter is correct under concurrency.
		var instanceId = Interlocked.Increment(ref _created);
		return new ExpensiveService(name, instanceId);
	}

	public static void ResetCounter()
	{
		// Reset between demos to keep results comparable.
		Interlocked.Exchange(ref _created, 0);
	}
}

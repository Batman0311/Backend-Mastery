namespace ResolverFactory.Services;

// Minimal contract so the demos can compare instance identity and type.
public interface IService
{
	// Name helps verify which implementation was selected by a factory.
	string Name { get; }
	// InstanceId is used to detect double initialization under concurrency.
	int InstanceId { get; }
}

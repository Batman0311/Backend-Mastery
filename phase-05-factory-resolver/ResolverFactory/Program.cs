using ResolverFactory.Demos;

// Mode is driven by the first CLI arg so each demo is isolated and repeatable.
var mode = args.Length > 0 ? args[0].Trim().ToLowerInvariant() : "help";

switch (mode)
{
		case "ifelse":
			// Simple factory selection by key with no concurrency concerns.
			ResolverDemo.RunIfElseFactory();
			break;
	case "naive":
			// Demonstrates the race when a cache is read and written without synchronization.
		ResolverDemo.RunNaiveRace();
		break;
	case "getoradd-bug":
			// Demonstrates that GetOrAdd can run a factory multiple times under contention.
		ResolverDemo.RunGetOrAddBug();
		break;
	case "lazy-fixed":
			// Demonstrates the fix: Lazy ensures one initialization for side-effectful factories.
		ResolverDemo.RunLazyFixed();
		break;
	default:
			// Unknown mode: show usage so the user can pick a demo.
		PrintUsage();
		break;
}

// Help output lives here so demo code stays focused on resolver behavior.
static void PrintUsage()
{
	// Keep help text centralized to avoid clutter in demo logic.
	Console.WriteLine("ResolverFactory modes:");
	Console.WriteLine("  ifelse");
	Console.WriteLine("  naive");
	Console.WriteLine("  getoradd-bug");
	Console.WriteLine("  lazy-fixed");
}

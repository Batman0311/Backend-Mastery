using PredicatePlayground.Demos;

// Mode is driven by the first CLI arg to keep the demos easy to run.
var mode = args.Length > 0 ? args[0].Trim().ToLowerInvariant() : "help";

switch (mode)
{
	case "where-bug":
		CustomWhereDemo.RunEagerBug();
		break;
	case "where-fixed":
		CustomWhereDemo.RunLazyFixed();
		break;
	case "predicate-bug":
		PredicateBuilderDemo.RunOrBug();
		break;
	case "predicate-fixed":
		PredicateBuilderDemo.RunOrFixed();
		break;
	default:
		PrintUsage();
		break;
}

// Help output lives here so demo code stays focused on predicate behavior.
static void PrintUsage()
{
	Console.WriteLine("PredicatePlayground modes:");
	Console.WriteLine("  where-bug");
	Console.WriteLine("  where-fixed");
	Console.WriteLine("  predicate-bug");
	Console.WriteLine("  predicate-fixed");
}

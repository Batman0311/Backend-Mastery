using PredicatePlayground.Demos;

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

static void PrintUsage()
{
	Console.WriteLine("PredicatePlayground modes:");
	Console.WriteLine("  where-bug");
	Console.WriteLine("  where-fixed");
	Console.WriteLine("  predicate-bug");
	Console.WriteLine("  predicate-fixed");
}

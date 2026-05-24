using OrderFulfillmentSaga.Demos;

// Mode is driven by the first CLI arg so each demo is isolated and repeatable.
var mode = args.Length > 0 ? args[0].Trim().ToLowerInvariant() : "help";

switch (mode)
{
	case "2pc-block":
		// Demonstrates 2PC blocking when the coordinator crashes after Prepare.
		TwoPhaseCommitDemo.RunCoordinatorCrashBlocking();
		break;
	case "3pc-precommit":
		// Demonstrates 3PC pre-commit with timeout-based completion.
		ThreePhaseCommitDemo.RunPreCommitFlow();
		break;
	case "saga-missing-comp":
		// Demonstrates missing compensation in a saga.
		SagaDemo.RunMissingCompensation();
		break;
	case "saga-compensated":
		// Demonstrates compensated saga rollback.
		SagaDemo.RunCompensated();
		break;
	default:
		// Unknown mode: show usage so the user can pick a demo.
		PrintUsage();
		break;
}

// Help output lives here so demo code stays focused on transaction behavior.
static void PrintUsage()
{
	// Keep help text centralized to avoid clutter in demo logic.
	Console.WriteLine("OrderFulfillmentSaga modes:");
	Console.WriteLine("  2pc-block");
	Console.WriteLine("  3pc-precommit");
	Console.WriteLine("  saga-missing-comp");
	Console.WriteLine("  saga-compensated");
}

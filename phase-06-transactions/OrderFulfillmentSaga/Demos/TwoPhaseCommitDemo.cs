namespace OrderFulfillmentSaga.Demos;

public static class TwoPhaseCommitDemo
{
	// Production scenario: a coordinator crashes after Prepare, leaving participants holding locks.
	public static void RunCoordinatorCrashBlocking()
	{
		RunCoordinatorCrashBlockingAsync().GetAwaiter().GetResult();
	}

	private static async Task RunCoordinatorCrashBlockingAsync()
	{
		var context = DemoContext.Create("2pc-block", "2pc", $"order-{Guid.NewGuid():N}");
		context.LogPath = PhaseLog.CreateFile(context.DemoMode, context.CorrelationId);

		// Enterprise scenario: payment, inventory, and shipping are locked while awaiting a global commit.
		DemoTelemetry.Log(
			"INFO",
			"coordinator",
			"demo.start",
			context,
			0,
			"starting",
			scenario: "Cross-service order fulfillment with inventory locked until a global commit decision is made.");

		using var rootSpan = DemoTelemetry.StartSpan("coordinator", "2pc", context);

		var participants = new List<CommitParticipant>
		{
			new("payment"),
			new("inventory"),
			new("shipping")
		};

		// Participants prepare in parallel to mimic concurrent service calls.
		context.Metrics.SetGauge("inflight_requests", participants.Count);
		var votes = await Task.WhenAll(participants.Select(participant => participant.PrepareAsync(context)));
		context.Metrics.SetGauge("inflight_requests", 0);

		if (!votes.All(vote => vote))
		{
			DemoTelemetry.Log("ERROR", "coordinator", "prepare", context, 0, "abort", failure: "vote_no");
			context.Metrics.Increment("errors_total");
			await Task.WhenAll(participants.Select(participant => participant.AbortAsync(context)));
			PrintSummary(context, participants);
			return;
		}

		// Timing window: the coordinator crashes after Prepare but before Commit/Abort.
		DemoTelemetry.Log("ERROR", "coordinator", "crash", context, 0, "expected_failure_coordinator_crash", failure: "coordinator_down");
		context.Metrics.Increment("errors_total");
		PhaseLog.Append(context.LogPath, "coordinator", "crash", "down", context.CorrelationId);

		PrintSummary(context, participants);
	}

	private static void PrintSummary(DemoContext context, IReadOnlyCollection<CommitParticipant> participants)
	{
		Console.WriteLine("Mode: 2pc-block");
		foreach (var participant in participants)
		{
			Console.WriteLine($"{participant.Name} state={participant.State} resourcesHeld={participant.ResourcesHeld}");
		}

		Console.WriteLine($"Phase log: {context.LogPath}");
		Console.WriteLine("Metrics to watch: requests_total, errors_total, request_duration_ms");
		context.Metrics.Print("coordinator", "2pc-block", context);
	}
}

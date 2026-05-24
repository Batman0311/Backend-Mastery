namespace OrderFulfillmentSaga.Demos;

public static class ThreePhaseCommitDemo
{
	// Production scenario: coordinator fails between Pre-Commit and Commit in a distributed update.
	public static void RunPreCommitFlow()
	{
		RunPreCommitFlowAsync().GetAwaiter().GetResult();
	}

	private static async Task RunPreCommitFlowAsync()
	{
		var context = DemoContext.Create("3pc-precommit", "3pc", $"order-{Guid.NewGuid():N}");
		context.LogPath = PhaseLog.CreateFile(context.DemoMode, context.CorrelationId);

		DemoTelemetry.Log(
			"INFO",
			"coordinator",
			"demo.start",
			context,
			0,
			"starting",
			scenario: "Multi-database update with a coordinator that can fail after pre-commit.");

		using var rootSpan = DemoTelemetry.StartSpan("coordinator", "3pc", context);

		var participants = new List<CommitParticipant>
		{
			new("payment"),
			new("inventory"),
			new("shipping")
		};

		// Prepare phase in parallel to overlap remote work.
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

		await Task.WhenAll(participants.Select(participant => participant.PreCommitAsync(context)));

		// Timing window: coordinator fails after Pre-Commit but before final Commit.
		DemoTelemetry.Log("WARN", "coordinator", "crash", context, 0, "expected_failure_coordinator_crash", failure: "coordinator_down");
		context.Metrics.Increment("errors_total");
		PhaseLog.Append(context.LogPath, "coordinator", "crash", "down", context.CorrelationId);

		var timeout = TimeSpan.FromMilliseconds(180);
		await Task.WhenAll(participants.Select(participant => participant.CommitAfterTimeoutAsync(context, timeout)));

		PrintSummary(context, participants);
	}

	private static void PrintSummary(DemoContext context, IReadOnlyCollection<CommitParticipant> participants)
	{
		Console.WriteLine("Mode: 3pc-precommit");
		foreach (var participant in participants)
		{
			Console.WriteLine($"{participant.Name} state={participant.State} resourcesHeld={participant.ResourcesHeld}");
		}

		Console.WriteLine($"Phase log: {context.LogPath}");
		Console.WriteLine("Metrics to watch: requests_total, errors_total, request_duration_ms");
		context.Metrics.Print("coordinator", "3pc-precommit", context);
	}
}

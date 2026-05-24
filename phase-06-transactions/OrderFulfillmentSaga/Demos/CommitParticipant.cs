using System.Diagnostics;

namespace OrderFulfillmentSaga.Demos;

internal enum ParticipantState
{
	Initial,
	Prepared,
	PreCommitted,
	Committed,
	Aborted
}

internal sealed class CommitParticipant
{
	public string Name { get; }
	public ParticipantState State { get; private set; } = ParticipantState.Initial;
	public bool ResourcesHeld { get; private set; }

	public CommitParticipant(string name)
	{
		Name = name;
	}

	public async Task<bool> PrepareAsync(DemoContext context)
	{
		// External call simulation so parallel prepares overlap in time.
		using var span = DemoTelemetry.StartSpan(Name, "prepare", context, "external");
		var stopwatch = Stopwatch.StartNew();
		await Task.Delay(60);
		stopwatch.Stop();

		State = ParticipantState.Prepared;
		ResourcesHeld = true;

		PhaseLog.Append(context.LogPath, Name, "prepare", "prepared", context.CorrelationId);
		DemoTelemetry.Log("INFO", Name, "prepare", context, stopwatch.ElapsedMilliseconds, "prepared", dependency: "external");
		context.Metrics.Increment("requests_total");
		context.Metrics.RecordDuration("request_duration_ms", stopwatch.ElapsedMilliseconds);
		return true;
	}

	public async Task PreCommitAsync(DemoContext context)
	{
		using var span = DemoTelemetry.StartSpan(Name, "precommit", context, "external");
		var stopwatch = Stopwatch.StartNew();
		await Task.Delay(50);
		stopwatch.Stop();

		State = ParticipantState.PreCommitted;

		PhaseLog.Append(context.LogPath, Name, "precommit", "precommitted", context.CorrelationId);
		DemoTelemetry.Log("INFO", Name, "precommit", context, stopwatch.ElapsedMilliseconds, "precommitted", dependency: "external");
		context.Metrics.Increment("requests_total");
		context.Metrics.RecordDuration("request_duration_ms", stopwatch.ElapsedMilliseconds);
	}

	public async Task CommitAsync(DemoContext context)
	{
		using var span = DemoTelemetry.StartSpan(Name, "commit", context, "external");
		var stopwatch = Stopwatch.StartNew();
		await Task.Delay(50);
		stopwatch.Stop();

		State = ParticipantState.Committed;
		ResourcesHeld = false;

		PhaseLog.Append(context.LogPath, Name, "commit", "committed", context.CorrelationId);
		DemoTelemetry.Log("INFO", Name, "commit", context, stopwatch.ElapsedMilliseconds, "committed", dependency: "external");
		context.Metrics.Increment("requests_total");
		context.Metrics.RecordDuration("request_duration_ms", stopwatch.ElapsedMilliseconds);
	}

	public async Task AbortAsync(DemoContext context)
	{
		using var span = DemoTelemetry.StartSpan(Name, "abort", context, "external");
		var stopwatch = Stopwatch.StartNew();
		await Task.Delay(40);
		stopwatch.Stop();

		State = ParticipantState.Aborted;
		ResourcesHeld = false;

		PhaseLog.Append(context.LogPath, Name, "abort", "aborted", context.CorrelationId);
		DemoTelemetry.Log("INFO", Name, "abort", context, stopwatch.ElapsedMilliseconds, "aborted", dependency: "external");
		context.Metrics.Increment("requests_total");
		context.Metrics.RecordDuration("request_duration_ms", stopwatch.ElapsedMilliseconds);
	}

	public async Task CommitAfterTimeoutAsync(DemoContext context, TimeSpan timeout)
	{
		// Timeout represents the 3PC window that triggers safe completion.
		await Task.Delay(timeout);
		await CommitAsync(context);
	}
}

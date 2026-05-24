using System.Diagnostics;

namespace OrderFulfillmentSaga.Demos;

public static class SagaDemo
{
	// Production scenario: payment succeeds, inventory fails, and no compensation is issued.
	public static void RunMissingCompensation()
	{
		RunMissingCompensationAsync().GetAwaiter().GetResult();
	}

	// Production scenario: failed steps trigger compensations in reverse order.
	public static void RunCompensated()
	{
		RunCompensatedAsync().GetAwaiter().GetResult();
	}

	private static async Task RunMissingCompensationAsync()
	{
		var context = DemoContext.Create("saga-missing-comp", "saga", $"order-{Guid.NewGuid():N}");
		context.LogPath = PhaseLog.CreateFile(context.DemoMode, context.CorrelationId);

		// Enterprise scenario: payment is captured and inventory fails with no automatic refund.
		DemoTelemetry.Log(
			"INFO",
			"saga",
			"demo.start",
			context,
			0,
			"starting",
			scenario: "Payment captured but inventory reservation fails without a compensating refund.");

		using var rootSpan = DemoTelemetry.StartSpan("saga", "orchestration", context);

		var steps = BuildSteps(failInventory: true);
		var succeeded = await ExecuteSagaAsync(steps, context, runCompensations: false);

		if (!succeeded)
		{
			DemoTelemetry.Log(
				"ERROR",
				"saga",
				"saga.failed",
				context,
				0,
				"expected_failure_missing_compensation",
				failure: "missing_compensation");
			context.Metrics.Increment("errors_total");
		}

		PrintSummary(context, "saga-missing-comp");
	}

	private static async Task RunCompensatedAsync()
	{
		var context = DemoContext.Create("saga-compensated", "saga", $"order-{Guid.NewGuid():N}");
		context.LogPath = PhaseLog.CreateFile(context.DemoMode, context.CorrelationId);

		DemoTelemetry.Log(
			"INFO",
			"saga",
			"demo.start",
			context,
			0,
			"starting",
			scenario: "Inventory failure triggers compensating actions to unwind payment and reservations.");

		using var rootSpan = DemoTelemetry.StartSpan("saga", "orchestration", context);

		var steps = BuildSteps(failInventory: true);
		await ExecuteSagaAsync(steps, context, runCompensations: true);

		PrintSummary(context, "saga-compensated");
	}

	private static List<SagaStep> BuildSteps(bool failInventory)
	{
		return new List<SagaStep>
		{
			new(
				"payment",
				"charge",
				async context => await ExecuteSagaOperationAsync(context, "payment", "charge", shouldFail: false),
				async context => await ExecuteSagaOperationAsync(context, "payment", "refund", shouldFail: false)),
			new(
				"inventory",
				"reserve",
				async context => await ExecuteSagaOperationAsync(context, "inventory", "reserve", shouldFail: failInventory),
				async context => await ExecuteSagaOperationAsync(context, "inventory", "release", shouldFail: false)),
			new(
				"shipping",
				"schedule",
				async context => await ExecuteSagaOperationAsync(context, "shipping", "schedule", shouldFail: false),
				async context => await ExecuteSagaOperationAsync(context, "shipping", "cancel", shouldFail: false))
		};
	}

	private static async Task<bool> ExecuteSagaAsync(List<SagaStep> steps, DemoContext context, bool runCompensations)
	{
		foreach (var step in steps)
		{
			var success = await step.ExecuteAsync(context);
			if (!success)
			{
				// Timing window: payment is captured before inventory fails, leaving partial side effects.
				DemoTelemetry.Log("WARN", "saga", "step.failed", context, 0, "step_failed", failure: step.Operation);

				if (runCompensations)
				{
					await CompensateAsync(steps, context);
				}

				return false;
			}
		}

		return true;
	}

	private static async Task CompensateAsync(List<SagaStep> steps, DemoContext context)
	{
		for (var index = steps.Count - 1; index >= 0; index--)
		{
			if (steps[index].Completed)
			{
				await steps[index].CompensateAsync(context);
			}
		}
	}

	private static async Task<bool> ExecuteSagaOperationAsync(
		DemoContext context,
		string service,
		string operation,
		bool shouldFail)
	{
		using var span = DemoTelemetry.StartSpan(service, operation, context, "external");
		var stopwatch = Stopwatch.StartNew();
		await Task.Delay(70);
		stopwatch.Stop();

		context.Metrics.Increment("requests_total");
		context.Metrics.RecordDuration("request_duration_ms", stopwatch.ElapsedMilliseconds);

		if (shouldFail)
		{
			context.Metrics.Increment("errors_total");
			DemoTelemetry.Log("ERROR", service, operation, context, stopwatch.ElapsedMilliseconds, "failed", dependency: "external", failure: "intentional");
			PhaseLog.Append(context.LogPath, service, operation, "failed", context.CorrelationId);
			return false;
		}

		DemoTelemetry.Log("INFO", service, operation, context, stopwatch.ElapsedMilliseconds, "completed", dependency: "external");
		PhaseLog.Append(context.LogPath, service, operation, "completed", context.CorrelationId);
		return true;
	}

	private static void PrintSummary(DemoContext context, string demoMode)
	{
		Console.WriteLine($"Mode: {demoMode}");
		Console.WriteLine($"Phase log: {context.LogPath}");
		Console.WriteLine("Metrics to watch: requests_total, errors_total, request_duration_ms");
		context.Metrics.Print("saga", demoMode, context);
	}
}

internal sealed class SagaStep
{
	private readonly Func<DemoContext, Task<bool>> _execute;
	private readonly Func<DemoContext, Task> _compensate;

	public SagaStep(string service, string operation, Func<DemoContext, Task<bool>> execute, Func<DemoContext, Task> compensate)
	{
		Service = service;
		Operation = operation;
		_execute = execute;
		_compensate = compensate;
	}

	public string Service { get; }
	public string Operation { get; }
	public bool Completed { get; private set; }

	public async Task<bool> ExecuteAsync(DemoContext context)
	{
		var success = await _execute(context);
		if (success)
		{
			Completed = true;
		}

		return success;
	}

	public async Task CompensateAsync(DemoContext context)
	{
		// Compensation must be idempotent because retries are common in production.
		await _compensate(context);
		DemoTelemetry.Log("INFO", Service, $"{Operation}.compensate", context, 0, "compensated");
		PhaseLog.Append(context.LogPath, Service, $"{Operation}.compensate", "completed", context.CorrelationId);
	}
}

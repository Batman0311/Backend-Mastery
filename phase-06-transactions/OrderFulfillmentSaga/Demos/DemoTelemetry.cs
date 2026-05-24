using System.Diagnostics;
using System.Globalization;

namespace OrderFulfillmentSaga.Demos;

internal sealed class DemoContext
{
	public string CorrelationId { get; }
	public string RequestId { get; }
	public string EntityId { get; }
	public string Phase { get; }
	public string DemoMode { get; }
	public string LogPath { get; set; } = string.Empty;
	public DemoMetrics Metrics { get; } = new();

	private DemoContext(string demoMode, string phase, string entityId)
	{
		DemoMode = demoMode;
		Phase = phase;
		EntityId = entityId;
		CorrelationId = Guid.NewGuid().ToString("N");
		RequestId = Guid.NewGuid().ToString("N");
	}

	public static DemoContext Create(string demoMode, string phase, string entityId)
	{
		return new DemoContext(demoMode, phase, entityId);
	}
}

internal sealed class DemoMetrics
{
	private readonly Dictionary<string, long> _counters = new(StringComparer.OrdinalIgnoreCase);
	private readonly Dictionary<string, long> _gauges = new(StringComparer.OrdinalIgnoreCase);
	private readonly Dictionary<string, List<long>> _timers = new(StringComparer.OrdinalIgnoreCase);

	public void Increment(string name, long value = 1)
	{
		if (!_counters.TryAdd(name, value))
		{
			_counters[name] += value;
		}
	}

	public void SetGauge(string name, long value)
	{
		_gauges[name] = value;
	}

	public void RecordDuration(string name, long durationMs)
	{
		if (!_timers.TryGetValue(name, out var values))
		{
			values = new List<long>();
			_timers[name] = values;
		}

		values.Add(durationMs);
	}

	public void Print(string service, string operation, DemoContext context)
	{
		foreach (var counter in _counters)
		{
			Console.WriteLine(
				$"metric={counter.Key} value={counter.Value} service={service} operation={operation} phase={context.Phase} demoMode={context.DemoMode}");
		}

		foreach (var gauge in _gauges)
		{
			Console.WriteLine(
				$"metric={gauge.Key} value={gauge.Value} service={service} operation={operation} phase={context.Phase} demoMode={context.DemoMode}");
		}

		foreach (var timer in _timers)
		{
			var ordered = timer.Value.OrderBy(value => value).ToArray();
			if (ordered.Length == 0)
			{
				continue;
			}

			var p95Index = (int)Math.Ceiling(0.95 * ordered.Length) - 1;
			var p95 = ordered[Math.Clamp(p95Index, 0, ordered.Length - 1)];
			var avg = ordered.Average();

			Console.WriteLine(
				$"metric={timer.Key} count={ordered.Length} p95={p95.ToString(CultureInfo.InvariantCulture)} avg={avg.ToString("F1", CultureInfo.InvariantCulture)} service={service} operation={operation} phase={context.Phase} demoMode={context.DemoMode}");
		}
	}
}

internal sealed class DemoSpan : IDisposable
{
	private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
	private readonly string _service;
	private readonly string _operation;
	private readonly DemoContext _context;
	private readonly string? _dependency;

	public DemoSpan(string service, string operation, DemoContext context, string? dependency)
	{
		_service = service;
		_operation = operation;
		_context = context;
		_dependency = dependency;
		DemoTelemetry.Log("INFO", _service, _operation, _context, 0, "span.start", dependency: _dependency);
	}

	public void Dispose()
	{
		_stopwatch.Stop();
		DemoTelemetry.Log("INFO", _service, _operation, _context, _stopwatch.ElapsedMilliseconds, "span.end", dependency: _dependency);
	}
}

internal static class DemoTelemetry
{
	public static DemoSpan StartSpan(string service, string operation, DemoContext context, string? dependency = null)
	{
		return new DemoSpan(service, operation, context, dependency);
	}

	public static void Log(
		string level,
		string service,
		string operation,
		DemoContext context,
		long durationMs,
		string result,
		string? scenario = null,
		string? dependency = null,
		string? failure = null)
	{
		var message =
			$"level={level} service={service} operation={operation} correlationId={context.CorrelationId} requestId={context.RequestId} entityId={context.EntityId} durationMs={durationMs} result={result}";

		if (!string.IsNullOrWhiteSpace(context.Phase))
		{
			message += $" phase={context.Phase}";
		}

		if (!string.IsNullOrWhiteSpace(context.DemoMode))
		{
			message += $" demoMode={context.DemoMode}";
		}

		if (!string.IsNullOrWhiteSpace(dependency))
		{
			message += $" dependency={dependency}";
		}

		if (!string.IsNullOrWhiteSpace(failure))
		{
			message += $" failure={failure}";
		}

		if (!string.IsNullOrWhiteSpace(scenario))
		{
			message += $" scenario=\"{scenario}\"";
		}

		Console.WriteLine(message);
	}
}

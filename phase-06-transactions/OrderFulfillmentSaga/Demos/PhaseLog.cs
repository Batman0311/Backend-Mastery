namespace OrderFulfillmentSaga.Demos;

internal static class PhaseLog
{
	private static readonly object Gate = new();

	public static string CreateFile(string demoMode, string correlationId)
	{
		var directory = Path.Combine(Environment.CurrentDirectory, "phase-logs");
		Directory.CreateDirectory(directory);

		var logPath = Path.Combine(directory, $"{demoMode}-{correlationId}.log");
		File.WriteAllText(logPath, $"# Phase log for {demoMode} correlationId={correlationId}{Environment.NewLine}");
		return logPath;
	}

	public static void Append(string logPath, string service, string phase, string state, string correlationId)
	{
		var line = $"{DateTimeOffset.UtcNow:O} service={service} phase={phase} state={state} correlationId={correlationId}";
		lock (Gate)
		{
			File.AppendAllText(logPath, line + Environment.NewLine);
		}
	}
}

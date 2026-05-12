using System;

namespace ParallelTaskProcessor;

internal static class Program
{
    private static void Main(string[] args)
    {
        // Normalize the mode so input like "RACE" or " race " still works.
        var mode = args.Length > 0 ? args[0].Trim().ToLowerInvariant() : "race";

        // Dispatch to a demo based on the mode argument.
        switch (mode)
        {
            case "race":
                InMemoryConcurrencyDemo.RunRaceConditionDemo();
                break;
            case "race-fixed":
                InMemoryConcurrencyDemo.RunRaceConditionFixed();
                break;
            case "race-lock":
                InMemoryConcurrencyDemo.RunRaceConditionFixedWithLock();
                break;
            case "race-concurrent":
                InMemoryConcurrencyDemo.RunConcurrentCollectionDemo();
                break;
            case "deadlock":
                InMemoryConcurrencyDemo.RunDeadlockDemo();
                break;
            case "deadlock-fixed":
                InMemoryConcurrencyDemo.RunDeadlockFixed();
                break;
            case "db-race":
                DatabaseConcurrencyDemo.RunDatabaseRaceConditionDemo();
                break;
            case "db-fixed":
                DatabaseConcurrencyDemo.RunDatabaseRaceConditionFixed();
                break;
            case "db-deadlock":
                DatabaseConcurrencyDemo.RunDatabaseDeadlockDemo();
                break;
            case "db-deadlock-fixed":
                DatabaseConcurrencyDemo.RunDatabaseDeadlockFixed();
                break;
            case "throughput":
                ThroughputDemo.RunThroughputComparison();
                break;
            case "help":
            case "-h":
            case "--help":
                PrintUsage();
                break;
            default:
                Console.WriteLine("Unknown mode.");
                PrintUsage();
                break;
        }
    }

    private static void PrintUsage()
    {
        Console.WriteLine("Usage:");
        Console.WriteLine("  dotnet run -- race");
        Console.WriteLine("  dotnet run -- race-fixed");
        Console.WriteLine("  dotnet run -- race-lock");
        Console.WriteLine("  dotnet run -- race-concurrent");
        Console.WriteLine("  dotnet run -- deadlock");
        Console.WriteLine("  dotnet run -- deadlock-fixed");
        Console.WriteLine("  dotnet run -- db-race");
        Console.WriteLine("  dotnet run -- db-fixed");
        Console.WriteLine("  dotnet run -- db-deadlock");
        Console.WriteLine("  dotnet run -- db-deadlock-fixed");
        Console.WriteLine("  dotnet run -- throughput");
    }
}

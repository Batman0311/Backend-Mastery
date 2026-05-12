using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace ParallelTaskProcessor;

internal static class ThroughputDemo
{
    private const int JobCount = 200;
    private const int WorkIterations = 2_000_00;

    public static void RunThroughputComparison()
    {
        Console.WriteLine("Throughput comparison (sequential vs parallel).");

        // Build a deterministic job list so work is comparable.
        var jobs = new int[JobCount];
        for (var i = 0; i < jobs.Length; i++)
        {
            jobs[i] = i;
        }

        // Run sequentially to establish a baseline.
        var sequential = Stopwatch.StartNew();
        foreach (var job in jobs)
        {
            CpuBoundWork(job);
        }
        sequential.Stop();

        // Run in parallel using the thread pool.
        var parallel = Stopwatch.StartNew();
        Parallel.ForEach(jobs, job =>
        {
            CpuBoundWork(job);
        });
        parallel.Stop();

        Console.WriteLine($"Sequential: {sequential.ElapsedMilliseconds} ms");
        Console.WriteLine($"Parallel:   {parallel.ElapsedMilliseconds} ms");
    }

    private static void CpuBoundWork(int jobId)
    {
        // Simulate CPU-bound work with deterministic arithmetic.
        var value = jobId;
        for (var i = 0; i < WorkIterations; i++)
        {
            value = (value * 31 + i) % 1_000_003;
        }
    }
}

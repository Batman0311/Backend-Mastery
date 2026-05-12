using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace ParallelTaskProcessor;

internal static class InMemoryConcurrencyDemo
{
    public static void RunRaceConditionDemo()
    {
        const int iterations = 1_000_000;

        Console.WriteLine("Race condition demo (intentionally broken).");
        Console.WriteLine($"Expected counter = {iterations}.");

        for (var trial = 1; trial <= 5; trial++)
        {
            var counter = 0;

            Parallel.For(0, iterations, _ =>
            {
                // Bug: ++ is not atomic; read-modify-write can interleave and lose updates.
                counter++;
            });

            Console.WriteLine($"Trial {trial}: Actual counter = {counter}");
        }

        Console.WriteLine("The mismatch is the race condition we will fix later.");
    }

    public static void RunRaceConditionFixed()
    {
        const int iterations = 1_000_000;

        Console.WriteLine("Race condition fixed with Interlocked.");
        Console.WriteLine($"Expected counter = {iterations}.");

        for (var trial = 1; trial <= 5; trial++)
        {
            var counter = 0;

            Parallel.For(0, iterations, _ =>
            {
                // Fix: Interlocked makes the increment atomic without a lock.
                Interlocked.Increment(ref counter);
            });

            Console.WriteLine($"Trial {trial}: Actual counter = {counter}");
        }
    }

    public static void RunDeadlockDemo()
    {
        Console.WriteLine("Deadlock demo (intentionally broken).");

        var lockA = new object();
        var lockB = new object();
        var barrier = new Barrier(2);

        var task1 = Task.Run(() =>
        {
            lock (lockA)
            {
                barrier.SignalAndWait();
                Thread.Sleep(50);
                // Bug: task1 holds lockA and waits for lockB.
                lock (lockB)
                {
                    Console.WriteLine("Task1 acquired both locks.");
                }
            }
        });

        var task2 = Task.Run(() =>
        {
            lock (lockB)
            {
                barrier.SignalAndWait();
                Thread.Sleep(50);
                // Bug: task2 holds lockB and waits for lockA, causing circular wait.
                lock (lockA)
                {
                    Console.WriteLine("Task2 acquired both locks.");
                }
            }
        });

        if (!Task.WaitAll(new[] { task1, task2 }, TimeSpan.FromSeconds(2)))
        {
            Console.WriteLine("Deadlock reproduced: tasks did not complete within timeout.");
            Console.WriteLine("We will fix this by enforcing lock ordering.");
        }
    }

    public static void RunDeadlockFixed()
    {
        Console.WriteLine("Deadlock fixed by enforcing lock ordering.");

        var lockA = new object();
        var lockB = new object();

        var task1 = Task.Run(() =>
        {
            lock (lockA)
            {
                Thread.Sleep(50);
                // Fix: both tasks take locks in the same order (A then B).
                lock (lockB)
                {
                    Console.WriteLine("Task1 acquired both locks.");
                }
            }
        });

        var task2 = Task.Run(() =>
        {
            lock (lockA)
            {
                Thread.Sleep(50);
                // Fix: same lock order as task1 to avoid circular wait.
                lock (lockB)
                {
                    Console.WriteLine("Task2 acquired both locks.");
                }
            }
        });

        Task.WaitAll(task1, task2);
        Console.WriteLine("Completed without deadlock.");
    }

    public static void RunRaceConditionFixedWithLock()
    {
        const int iterations = 1_000_000;

        Console.WriteLine("Race condition fixed with lock.");
        Console.WriteLine($"Expected counter = {iterations}.");

        for (var trial = 1; trial <= 5; trial++)
        {
            var counter = 0;
            var gate = new object();

            Parallel.For(0, iterations, _ =>
            {
                // Fix: lock serializes access to the shared counter.
                lock (gate)
                {
                    counter++;
                }
            });

            Console.WriteLine($"Trial {trial}: Actual counter = {counter}");
        }
    }

    public static void RunConcurrentCollectionDemo()
    {
        const int iterations = 1_000_000;

        Console.WriteLine("Concurrent collection demo with ConcurrentBag.");
        Console.WriteLine($"Expected count = {iterations}.");

        var bag = new ConcurrentBag<int>();

        Parallel.For(0, iterations, i =>
        {
            // ConcurrentBag handles synchronization internally for Add.
            bag.Add(i);
        });

        Console.WriteLine($"Actual count = {bag.Count}");
    }
}

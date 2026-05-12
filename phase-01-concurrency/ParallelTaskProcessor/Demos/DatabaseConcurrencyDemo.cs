using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ParallelTaskProcessor.Data;
using ParallelTaskProcessor.Models;

namespace ParallelTaskProcessor;

internal static class DatabaseConcurrencyDemo
{
    public static void RunDatabaseRaceConditionDemo()
    {
        Console.WriteLine("Database race condition demo (intentionally broken).");

        using var context = CreateContext();
        ResetInventory(context, sku1Quantity: 1, sku2Quantity: 1);

        var successes = 0;
        var tasks = new Task[10];

        for (var i = 0; i < tasks.Length; i++)
        {
            tasks[i] = Task.Run(() =>
            {
                using var worker = CreateContext();
                var item = worker.InventoryItems.Single(item => item.Sku == "SKU-1");

                if (item.Quantity <= 0)
                {
                    return;
                }

                // Bug: read-then-write without a concurrency check.
                var newQuantity = item.Quantity - 1;
                Thread.Sleep(30);

                worker.Database.ExecuteSqlRaw(
                    "UPDATE InventoryItems SET Quantity = {0} WHERE Id = {1}",
                    newQuantity,
                    item.Id);

                Interlocked.Increment(ref successes);
            });
        }

        Task.WaitAll(tasks);

        using var finalContext = CreateContext();
        var finalQuantity = finalContext.InventoryItems.Single(item => item.Sku == "SKU-1").Quantity;
        Console.WriteLine("Initial quantity: 1");
        Console.WriteLine($"Successful reservations: {successes}");
        Console.WriteLine($"Final quantity in DB: {finalQuantity}");
        Console.WriteLine("Oversell indicates the race condition.");
    }

    public static void RunDatabaseRaceConditionFixed()
    {
        Console.WriteLine("Database race condition fixed with optimistic concurrency.");

        using var context = CreateContext();
        ResetInventory(context, sku1Quantity: 1, sku2Quantity: 1);

        var successes = 0;
        var tasks = new Task[10];

        for (var i = 0; i < tasks.Length; i++)
        {
            tasks[i] = Task.Run(() =>
            {
                var retries = 0;

                while (retries < 5)
                {
                    using var worker = CreateContext();
                    var item = worker.InventoryItems.Single(item => item.Sku == "SKU-1");

                    if (item.Quantity <= 0)
                    {
                        return;
                    }

                    item.Quantity -= 1;
                    item.Version += 1;

                    try
                    {
                        worker.SaveChanges();
                        Interlocked.Increment(ref successes);
                        return;
                    }
                    catch (DbUpdateConcurrencyException)
                    {
                        retries++;
                    }
                }
            });
        }

        Task.WaitAll(tasks);

        using var finalContext = CreateContext();
        var finalQuantity = finalContext.InventoryItems.Single(item => item.Sku == "SKU-1").Quantity;
        Console.WriteLine("Initial quantity: 1");
        Console.WriteLine($"Successful reservations: {successes}");
        Console.WriteLine($"Final quantity in DB: {finalQuantity}");
        Console.WriteLine("No oversell indicates the fix is working.");
    }

    public static void RunDatabaseDeadlockDemo()
    {
        Console.WriteLine("Database deadlock-style demo (intentionally broken)." );
        Console.WriteLine("SQLite uses database-level locks, so we reproduce lock contention.");

        using var context = CreateContext();
        ResetInventory(context, sku1Quantity: 5, sku2Quantity: 5);

        var failures = 0;
        var task1 = Task.Run(() => RunTwoStepReservation("SKU-1", "SKU-2", ref failures));
        var task2 = Task.Run(() => RunTwoStepReservation("SKU-2", "SKU-1", ref failures));

        Task.WaitAll(task1, task2);

        Console.WriteLine($"Lock failures: {failures}");
        Console.WriteLine("Opposite lock order increases lock contention and can deadlock on DBs with row locks.");
    }

    public static void RunDatabaseDeadlockFixed()
    {
        Console.WriteLine("Database deadlock-style demo fixed with lock ordering." );
        Console.WriteLine("We keep the same order for both operations and use a small retry for lock timeouts.");

        using var context = CreateContext();
        ResetInventory(context, sku1Quantity: 5, sku2Quantity: 5);

        var failures = 0;
        var task1 = Task.Run(() => RunTwoStepReservation("SKU-1", "SKU-2", ref failures, sameOrder: true));
        var task2 = Task.Run(() => RunTwoStepReservation("SKU-1", "SKU-2", ref failures, sameOrder: true));

        Task.WaitAll(task1, task2);

        Console.WriteLine($"Lock failures: {failures}");
        Console.WriteLine("Consistent ordering reduces contention; retries handle transient locks.");
    }

    private static AppDbContext CreateContext()
    {
        var dbPath = Path.Combine(AppContext.BaseDirectory, "phase1.db");
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite($"Data Source={dbPath}")
            .Options;

        return new AppDbContext(options);
    }

    private static void ResetInventory(AppDbContext context, int sku1Quantity, int sku2Quantity)
    {
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();

        context.InventoryItems.Add(new InventoryItem
        {
            Sku = "SKU-1",
            Quantity = sku1Quantity,
            Version = 1
        });

        context.InventoryItems.Add(new InventoryItem
        {
            Sku = "SKU-2",
            Quantity = sku2Quantity,
            Version = 1
        });

        context.SaveChanges();
    }

    private static void RunTwoStepReservation(string firstSku, string secondSku, ref int failures, bool sameOrder = false)
    {
        var attempts = 0;

        while (attempts < 3)
        {
            try
            {
                using var worker = CreateContext();
                worker.Database.ExecuteSqlRaw("PRAGMA busy_timeout = 2000");

                using var transaction = worker.Database.BeginTransaction();

                ReserveOne(worker, firstSku);
                Thread.Sleep(200);

                var second = sameOrder ? "SKU-2" : secondSku;
                ReserveOne(worker, second);

                transaction.Commit();
                return;
            }
            catch (SqliteException ex) when (ex.SqliteErrorCode == 5)
            {
                attempts++;
                Thread.Sleep(100);
            }
            catch (DbUpdateException)
            {
                attempts++;
            }
        }

        Interlocked.Increment(ref failures);
    }

    private static void ReserveOne(AppDbContext context, string sku)
    {
        var item = context.InventoryItems.Single(item => item.Sku == sku);
        if (item.Quantity <= 0)
        {
            return;
        }

        item.Quantity -= 1;
        item.Version += 1;
        context.SaveChanges();
    }
}

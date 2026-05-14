using System;
using System.Collections.Generic;
using System.Linq;

namespace PredicatePlayground.Demos;

public static class CustomWhereDemo
{
    // Production scenario: a batch export pipeline filters a large sequence before streaming it out.
    public static void RunEagerBug()
    {
        Console.WriteLine("Mode: where-bug");

        var numbers = Enumerable.Range(1, 5);
        var filtered = numbers.CustomWhereEager(n =>
        {
            Console.WriteLine($"Evaluating {n}");
            return n % 2 == 0;
        });

        Console.WriteLine("Filter built; no enumeration should have happened yet.");
        Console.WriteLine("Iterating results:");

        foreach (var n in filtered)
        {
            Console.WriteLine($"Result {n}");
        }
    }

    // Production scenario: a streaming pipeline evaluates filters only when a consumer pulls data.
    public static void RunLazyFixed()
    {
        Console.WriteLine("Mode: where-fixed");

        var numbers = Enumerable.Range(1, 5);
        var filtered = numbers.CustomWhereLazy(n =>
        {
            Console.WriteLine($"Evaluating {n}");
            return n % 2 == 0;
        });

        Console.WriteLine("Filter built; enumeration triggers evaluation.");
        Console.WriteLine("Iterating results:");

        foreach (var n in filtered)
        {
            Console.WriteLine($"Result {n}");
        }
    }
}

public static class WhereExtensions
{
    public static IEnumerable<int> CustomWhereEager(this IEnumerable<int> source, Func<int, bool> predicate)
    {
        if (source is null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        if (predicate is null)
        {
            throw new ArgumentNullException(nameof(predicate));
        }

        var results = new List<int>();

        // Intentional bug: eager evaluation materializes results immediately.
        foreach (var item in source)
        {
            if (predicate(item))
            {
                results.Add(item);
            }
        }

        return results;
    }

    public static IEnumerable<int> CustomWhereLazy(this IEnumerable<int> source, Func<int, bool> predicate)
    {
        if (source is null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        if (predicate is null)
        {
            throw new ArgumentNullException(nameof(predicate));
        }

        return CustomWhereLazyIterator(source, predicate);
    }

    private static IEnumerable<int> CustomWhereLazyIterator(IEnumerable<int> source, Func<int, bool> predicate)
    {
        foreach (var item in source)
        {
            if (predicate(item))
            {
                yield return item;
            }
        }
    }
}

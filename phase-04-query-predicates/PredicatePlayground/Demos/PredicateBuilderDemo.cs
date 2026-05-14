using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace PredicatePlayground.Demos;

public static class PredicateBuilderDemo
{
    // Production scenario: UI filters are composed dynamically for search requests.
    public static void RunOrBug()
    {
        Console.WriteLine("Mode: predicate-bug");

        var products = SampleProducts();

        Expression<Func<Product, bool>> inStock = product => product.Stock > 0;
        Expression<Func<Product, bool>> nameHasA = product => product.Name.Contains('A');

        // Intentional bug: combining expressions without rebinding parameters.
        var combined = PredicateBuilder.OrBug(inStock, nameHasA);

        try
        {
            var result = products.AsQueryable().Where(combined).ToList();
            PrintResults(result);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Exception while executing predicate composition:");
            Console.WriteLine(ex.Message);
        }
    }

    // Production scenario: search filters need safe composition for query providers.
    public static void RunOrFixed()
    {
        Console.WriteLine("Mode: predicate-fixed");

        var products = SampleProducts();

        Expression<Func<Product, bool>> inStock = product => product.Stock > 0;
        Expression<Func<Product, bool>> nameHasA = product => product.Name.Contains('A');

        var combined = PredicateBuilder.OrFixed(inStock, nameHasA);
        var result = products.AsQueryable().Where(combined).ToList();

        PrintResults(result);
    }

    private static List<Product> SampleProducts()
    {
        return new List<Product>
        {
            new("ALPHA", 5),
            new("BETA", 0),
            new("OMEGA", 3),
            new("SIGMA", 0)
        };
    }

    private static void PrintResults(IEnumerable<Product> products)
    {
        foreach (var product in products)
        {
            Console.WriteLine($"Match: {product.Name} (Stock: {product.Stock})");
        }
    }
}

public static class PredicateBuilder
{
    public static Expression<Func<T, bool>> OrBug<T>(Expression<Func<T, bool>> left, Expression<Func<T, bool>> right)
    {
        var body = Expression.OrElse(left.Body, right.Body);
        return Expression.Lambda<Func<T, bool>>(body, left.Parameters);
    }

    public static Expression<Func<T, bool>> OrFixed<T>(Expression<Func<T, bool>> left, Expression<Func<T, bool>> right)
    {
        var parameter = Expression.Parameter(typeof(T), "x");
        var leftBody = new ParameterReplacer(left.Parameters[0], parameter).Visit(left.Body);
        var rightBody = new ParameterReplacer(right.Parameters[0], parameter).Visit(right.Body);

        var body = Expression.OrElse(leftBody!, rightBody!);
        return Expression.Lambda<Func<T, bool>>(body, parameter);
    }

    private sealed class ParameterReplacer : ExpressionVisitor
    {
        private readonly ParameterExpression _source;
        private readonly ParameterExpression _target;

        public ParameterReplacer(ParameterExpression source, ParameterExpression target)
        {
            _source = source;
            _target = target;
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            return node == _source ? _target : base.VisitParameter(node);
        }
    }
}

public sealed record Product(string Name, int Stock);

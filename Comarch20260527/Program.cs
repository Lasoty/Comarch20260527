using System.Collections.Concurrent;

internal static class Program
{
    private const int Iterations = 50_000;

    public static async Task Main()
    {
        Console.WriteLine($"Expected result: {Iterations}");
        Console.WriteLine();

        RunBrokenCounter();
        RunWithLock();
        RunWithInterlocked();
        await RunWithSemaphoreSlimAsync();
        RunWithConcurrentDictionary();
    }

    private static void RunBrokenCounter()
    {
        var counter = 0;

        Parallel.For(0, Iterations, _ =>
        {
            counter++;
        });

        Console.WriteLine($"Broken counter:             {counter}");
    }

    private static void RunWithLock()
    {
        var counter = 0;
        object sync = new();

        Parallel.For(0, Iterations, _ =>
        {
            lock (sync)
            {
                counter++;
            }
        });

        Console.WriteLine($"Counter with lock:             {counter}");
    }

    private static void RunWithInterlocked()
    {
        var counter = 0;
        Parallel.For(0, Iterations, _ =>
        {
            Interlocked.Increment(ref counter);
        });
        Console.WriteLine($"Counter with Interlock:             {counter}");
    }

    private static async Task RunWithSemaphoreSlimAsync()
    {
        var counter = 0;
        using var semaphore = new SemaphoreSlim(1, 1);
        var tasks = Enumerable.Range(0, Iterations).Select(async _ =>
        {
            await semaphore.WaitAsync();
            try
            {
                counter++;
            }
            finally
            {
                semaphore.Release();
            }
        });
        await Task.WhenAll(tasks);
        Console.WriteLine($"Counter with semaphoreSlim:             {counter}");
    }

    private static void RunWithConcurrentDictionary()
    {
        var dictionary = new ConcurrentDictionary<string, int>();
        Parallel.For(0, Iterations, _ =>
        {
            dictionary.AddOrUpdate("counter", 1, (_, oldvalue) => oldvalue + 1);
        });
        Console.WriteLine($"Counter in dictionary:             {dictionary["counter"]}");
    }
}
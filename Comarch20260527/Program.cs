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

    }

    private static void RunWithInterlocked()
    {

    }

    private static async Task RunWithSemaphoreSlimAsync()
    {

    }

    private static void RunWithConcurrentDictionary()
    {

    }
}
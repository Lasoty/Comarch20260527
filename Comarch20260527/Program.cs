/*
   Powiązany slajd: Ćwiczenie: równoległe przetwarzanie danych
   Cel: Porównać wersję sekwencyjną, Parallel.ForEach i Parallel.ForEachAsync oraz wpływ MaxDegreeOfParallelism.

   Wprowadzenie dla uczestników
   Nie każda równoległość przyspiesza aplikację.
   W tym ćwiczeniu uczestnicy porównają kilka wariantów przetwarzania tej samej kolekcji.
   Celem jest zobaczenie, że Parallel pasuje do pracy CPU-bound, a Parallel.ForEachAsync może być
   czytelnym sposobem kontrolowania współbieżności przy operacjach asynchronicznych.

   
  
   3. Dodać `ParallelOptions` z `MaxDegreeOfParallelism`.
   4. Porównać wyniki dla limitów `2`, `4`, `Environment.ProcessorCount`.
   5. Wyjaśnić, kiedy równoległość może zaszkodzić.

*/

using System.Collections.Concurrent;
using System.Diagnostics;

internal static class Program
{
    public static async Task Main()
    {
        var numbers = Enumerable.Range(35_000, 20_000).ToArray();

        Measure("Sequential CPU", () =>
        {
            var result = numbers.Select(CpuIntensiveWork).ToList();
            Console.WriteLine($"Result: {result.Count}");
        });

        Measure("Parallel.ForEach CPU", () =>
        {
            var result = new ConcurrentBag<int>();
            var options = new ParallelOptions
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount
            };

            Parallel.ForEach(numbers, options, number =>
            {
                result.Add(CpuIntensiveWork(number));
            });
        });

        await MeasureAsync($"Parallel.ForEachAsync", async () =>
        {
            var result = new ConcurrentBag<int>();
            var options = new ParallelOptions
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount
            };

            await Parallel.ForEachAsync(numbers, options, async (number, token) =>
            {
                await Task.Delay(1, token);
                result.Add(CpuIntensiveWork(number));
            });
        });
    }

    public static int CpuIntensiveWork(int value)
    {
        var result = 0;
        for (int i = 2; i < 250; i++)
        {
            if (value % i == 0)
            {
                result++;
            }
        }

        return result;
    }

    private static void Measure(string name, Action action)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        action();
        Console.WriteLine($"{name}: {stopwatch.ElapsedMilliseconds} ms");
        Console.WriteLine();
    }

    private static async Task MeasureAsync(string name, Func<Task> action)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        await action();
        Console.WriteLine($"{name}: {stopwatch.ElapsedMilliseconds} ms");
        Console.WriteLine();
    }
}
/*
   Powiązany slajd: Ćwiczenie: bounded Channel
   Cel: Zbudować bounded channel z wieloma producentami i konsumentami, anulowaniem oraz raportem błędów.

   Wprowadzenie dla uczestników
   Channel<T> umożliwia asynchroniczne przekazywanie danych między producentami i konsumentami.
   Wersja bounded ogranicza pojemność kanału i wprowadza backpressure. To chroni aplikację przed
   niekontrolowanym wzrostem pamięci, gdy producent generuje dane szybciej niż konsumenci je przetwarzają.

   Zadania dla uczestników

   1. Zmienić kanał na bounded o pojemności `10`.
   2. Dodać trzech producentów.
   3. Dodać czterech konsumentów.
   4. Dodać `CancellationTokenSource` z timeoutem `10 sekund`.
   5. Dodać licznik przetworzonych zadań i licznik błędów.
   6. Upewnić się, że `Writer.Complete()` jest wywołany po zakończeniu wszystkich producentów.
*/

using System.Threading.Channels;

internal static class Program
{
    public static async Task Main()
    {
        var channel = Channel.CreateUnbounded<Job>();

        var producer = Task.Run(async () =>
        {
            for (var i = 1; i <= 50; i++)
            {
                await channel.Writer.WriteAsync(new Job(i));
                Console.WriteLine($"Produced {i}");
            }

            channel.Writer.Complete();
        });

        var consumer = Task.Run(async () =>
        {
            await foreach (var job in channel.Reader.ReadAllAsync())
            {
                await ProcessAsync(job);
                Console.WriteLine($"Processed {job.Id}");
            }
        });

        await Task.WhenAll(producer, consumer);
    }

    private static async Task ProcessAsync(Job job)
    {
        await Task.Delay(Random.Shared.Next(40, 120));
    }
}

internal sealed record Job(int Id);
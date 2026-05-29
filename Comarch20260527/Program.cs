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
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        var channel = Channel.CreateBounded<Job>(new BoundedChannelOptions(10)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = false,
            SingleWriter = false
        });

        var processed = 0;
        var failed = 0;

        var producers = Enumerable.Range(1, 3)
            .Select(producerId => ProduceAsync(producerId, channel.Writer, cts.Token))
            .ToList();

        var consumers = Enumerable.Range(1, 4)
            .Select(consumerId => ConsumeAsync(consumerId, channel.Reader, cts.Token, () =>
            {
                Interlocked.Increment(ref processed);
            }, () =>
            {
                Interlocked.Increment(ref failed);
            }))
            .ToList();

        try
        {
            await Task.WhenAll(producers);
            channel.Writer.Complete();
            await Task.WhenAll(consumers);
        }
        catch (OperationCanceledException)
        {
            channel.Writer.TryComplete();
            Console.WriteLine("Pipeline cancelled.");
        }

        Console.WriteLine();
        Console.WriteLine($"Processed: {processed}");
        Console.WriteLine($"Failed:    {failed}");
    }

    private static async Task ProduceAsync(
        int producerId,
        ChannelWriter<Job> writer,
        CancellationToken cancellationToken)
    {
        for (var i = 1; i <= 25; i++)
        {
            var job = new Job((producerId * 100) + i, producerId);
            await writer.WriteAsync(job, cancellationToken);
            Console.WriteLine($"Producer {producerId}: produced job {job.Id}");
            await Task.Delay(Random.Shared.Next(10, 50), cancellationToken);
        }
    }

    private static async Task ConsumeAsync(
        int consumerId,
        ChannelReader<Job> reader,
        CancellationToken cancellationToken,
        Action onProcessed,
        Action onFailed)
    {
        await foreach (var job in reader.ReadAllAsync(cancellationToken))
        {
            try
            {
                await ProcessAsync(job, cancellationToken);
                onProcessed();
                Console.WriteLine($"Consumer {consumerId}: processed job {job.Id}");
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                onFailed();
                Console.WriteLine($"Consumer {consumerId}: job {job.Id} failed: {ex.Message}");
            }
        }
    }

    private static async Task ProcessAsync(Job job, CancellationToken cancellationToken)
    {
        await Task.Delay(Random.Shared.Next(40, 120), cancellationToken);

        if (job.Id % 17 == 0)
        {
            throw new InvalidOperationException("Simulated processing error.");
        }
    }
}

internal sealed record Job(int Id, int ProducerId);